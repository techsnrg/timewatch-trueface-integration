from datetime import datetime

from trueface_integration.services.normalization import (
	build_stable_key,
	normalize_log_type,
	normalize_punch,
	parse_punch_time,
)


def test_normalize_log_type_from_direction():
	assert normalize_log_type(direction="ENTRY") == "IN"
	assert normalize_log_type(direction="EXIT") == "OUT"


def test_normalize_log_type_from_attendance_state():
	assert normalize_log_type(attendance_state="SIGNIN") == "IN"
	assert normalize_log_type(attendance_state="WORK_OVERTIME_SIGNOUT") == "OUT"


def test_parse_punch_time_accepts_iso_and_common_formats():
	assert parse_punch_time("2026-05-04T09:15:00") == datetime(2026, 5, 4, 9, 15)
	assert parse_punch_time("2026-05-04 09:15:00") == datetime(2026, 5, 4, 9, 15)
	assert parse_punch_time("04-05-2026 09:15:00") == datetime(2026, 5, 4, 9, 15)


def test_stable_key_prefers_record_number():
	key = build_stable_key(
		device_serial="TF3000-001",
		user_id="EMP-001",
		punch_time=datetime(2026, 5, 4, 9, 15),
		direction="ENTRY",
		record_number="123",
	)
	assert key == "TF3000-001:record:123"


def test_fallback_stable_key_is_stable():
	punch_time = datetime(2026, 5, 4, 9, 15)
	first = build_stable_key("TF3000-001", "EMP-001", punch_time, "ENTRY")
	second = build_stable_key("TF3000-001", "EMP-001", punch_time, "ENTRY")
	assert first == second
	assert first.startswith("TF3000-001:fallback:")


def test_normalize_punch_maps_sdk_aliases():
	punch = normalize_punch(
		"TF3000-001",
		{
			"szSN": "TF3000-001",
			"nRecNo": 42,
			"szUserID": "EMP-001",
			"szCardNo": "CARD-9",
			"time": "2026-05-04 09:15:00",
			"emDirection": "ENTRY",
			"emAttendanceState": "SIGNIN",
			"bStatus": True,
		},
	)

	assert punch["stable_key"] == "TF3000-001:record:42"
	assert punch["user_id"] == "EMP-001"
	assert punch["card_no"] == "CARD-9"
	assert punch["log_type"] == "IN"
	assert punch["punch_time"] == datetime(2026, 5, 4, 9, 15)
