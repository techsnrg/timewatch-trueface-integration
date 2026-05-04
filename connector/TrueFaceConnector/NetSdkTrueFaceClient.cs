namespace TrueFaceConnector;

#if TRUEFACE_NETSDK
using NetSDKCS;
using System.Runtime.InteropServices;

public sealed class NetSdkTrueFaceClient : ITrueFaceSdkClient
{
    private IntPtr _loginId = IntPtr.Zero;
    private IntPtr _realLoadId = IntPtr.Zero;
    private fAnalyzerDataCallBack? _callback;
    private Func<PunchRecord, Task>? _onPunch;
    private DeviceOptions? _device;

    public Task ConnectAsync(DeviceOptions device, CancellationToken cancellationToken)
    {
        _device = device;
        NETClient.Init(null, IntPtr.Zero, null);
        NET_DEVICEINFO_Ex deviceInfo = new();
        _loginId = NETClient.LoginWithHighLevelSecurity(
            device.IpAddress,
            (ushort)device.Port,
            device.Username,
            device.Password,
            EM_LOGIN_SPAC_CAP_TYPE.TCP,
            IntPtr.Zero,
            ref deviceInfo);

        if (_loginId == IntPtr.Zero)
        {
            throw new InvalidOperationException($"TrueFace login failed for {device.DeviceId} at {device.IpAddress}:{device.Port}.");
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PunchRecord>> QueryRecordsAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        NET_FIND_RECORD_ACCESSCTLCARDREC_CONDITION_EX condition = new()
        {
            dwSize = (uint)Marshal.SizeOf<NET_FIND_RECORD_ACCESSCTLCARDREC_CONDITION_EX>(),
            bTimeEnable = true,
            stStartTime = NET_TIME.FromDateTime(from),
            stEndTime = NET_TIME.FromDateTime(to),
        };

        IntPtr findId = IntPtr.Zero;
        bool started = NETClient.FindRecord(
            _loginId,
            EM_NET_RECORD_TYPE.ACCESSCTLCARDREC_EX,
            condition,
            typeof(NET_FIND_RECORD_ACCESSCTLCARDREC_CONDITION_EX),
            ref findId,
            10000);

        if (!started || findId == IntPtr.Zero)
        {
            return Task.FromResult<IReadOnlyList<PunchRecord>>([]);
        }

        List<PunchRecord> records = [];
        try
        {
            const int batchSize = 50;
            int retNum = 0;
            List<object> sdkRows = Enumerable.Range(0, batchSize)
                .Select(_ => (object)new NET_RECORDSET_ACCESS_CTL_CARDREC())
                .ToList();

            while (!cancellationToken.IsCancellationRequested)
            {
                int ret = NETClient.FindNextRecord(
                    findId,
                    batchSize,
                    ref retNum,
                    ref sdkRows,
                    typeof(NET_RECORDSET_ACCESS_CTL_CARDREC),
                    10000);

                if (ret < 0 || retNum == 0)
                {
                    break;
                }

                records.AddRange(sdkRows.Cast<NET_RECORDSET_ACCESS_CTL_CARDREC>().Select(MapRecord));
                sdkRows = Enumerable.Range(0, batchSize)
                    .Select(_ => (object)new NET_RECORDSET_ACCESS_CTL_CARDREC())
                    .ToList();
            }
        }
        finally
        {
            NETClient.FindRecordClose(findId);
        }

        return Task.FromResult<IReadOnlyList<PunchRecord>>(records);
    }

    public Task SubscribeAsync(Func<PunchRecord, Task> onPunch, CancellationToken cancellationToken)
    {
        _onPunch = onPunch;
        _callback = AnalyzerDataCallback;
        _realLoadId = NETClient.RealLoadPicture(_loginId, 0, (uint)EM_EVENT_IVS_TYPE.ACCESS_CTL, true, _callback, _loginId, IntPtr.Zero);
        if (_realLoadId == IntPtr.Zero)
        {
            throw new InvalidOperationException("TrueFace live event subscription failed.");
        }
        return Task.CompletedTask;
    }

    private int AnalyzerDataCallback(IntPtr handle, uint eventType, IntPtr eventInfo, IntPtr buffer, uint bufferSize, IntPtr user, int sequence, IntPtr reserved)
    {
        if (eventType == (uint)EM_EVENT_IVS_TYPE.ACCESS_CTL && _onPunch is not null)
        {
            NET_DEV_EVENT_ACCESS_CTL_INFO info = Marshal.PtrToStructure<NET_DEV_EVENT_ACCESS_CTL_INFO>(eventInfo);
            _ = _onPunch(MapEvent(info));
        }
        return 1;
    }

    private PunchRecord MapRecord(NET_RECORDSET_ACCESS_CTL_CARDREC row)
    {
        return new PunchRecord
        {
            DeviceSerial = row.szSN ?? _device?.DeviceId ?? "",
            RecordNumber = row.nRecNo.ToString(),
            UserId = row.szUserID ?? "",
            CardNo = row.szCardNo,
            PunchTime = ToDateTime(row.stuTime),
            Direction = row.emDirection.ToString(),
            AttendanceState = row.emAttendanceState.ToString(),
            Status = row.bStatus,
            OpenMethod = row.emMethod.ToString(),
            ErrorCode = row.nErrorCode.ToString(),
        };
    }

    private PunchRecord MapEvent(NET_DEV_EVENT_ACCESS_CTL_INFO info)
    {
        return new PunchRecord
        {
            DeviceSerial = info.szSN ?? _device?.DeviceId ?? "",
            RecordNumber = info.nPunchingRecNo > 0 ? info.nPunchingRecNo.ToString() : null,
            EventId = info.nEventID.ToString(),
            UserId = info.szUserID ?? "",
            CardNo = info.szCardNo,
            PunchTime = ToDateTime(info.UTC),
            Direction = null,
            AttendanceState = info.emAttendanceState.ToString(),
            Status = info.bStatus,
            OpenMethod = info.emOpenMethod.ToString(),
            ErrorCode = info.nErrorCode.ToString(),
        };
    }

    private static DateTime ToDateTime(NET_TIME time)
    {
        return new DateTime((int)time.dwYear, (int)time.dwMonth, (int)time.dwDay, (int)time.dwHour, (int)time.dwMinute, (int)time.dwSecond);
    }

    private static DateTime ToDateTime(NET_TIME_EX time)
    {
        return new DateTime((int)time.dwYear, (int)time.dwMonth, (int)time.dwDay, (int)time.dwHour, (int)time.dwMinute, (int)time.dwSecond);
    }

    public ValueTask DisposeAsync()
    {
        if (_realLoadId != IntPtr.Zero)
        {
            NETClient.StopLoadPic(_realLoadId);
            _realLoadId = IntPtr.Zero;
        }
        if (_loginId != IntPtr.Zero)
        {
            NETClient.Logout(_loginId);
            _loginId = IntPtr.Zero;
        }
        NETClient.Cleanup();
        return ValueTask.CompletedTask;
    }
}
#endif
