import hashlib
from datetime import datetime


IN_VALUES = {"ENTRY", "IN", "I", "SIGNIN", "SIGN_IN", "CHECKIN", "PUNCH IN", "WORK_OVERTIME_SIGNIN"}
OUT_VALUES = {"EXIT", "OUT", "O", "SIGNOUT", "SIGN_OUT", "CHECKOUT", "PUNCH OUT", "WORK_OVERTIME_SIGNOUT"}


def normalize_text(value):
	if value is None:
		return ""
	return str(value).strip()


def normalize_log_type(direction=None, attendance_state=None):
	values = [normalize_text(direction), normalize_text(attendance_state)]
	for value in values:
		key = value.upper().replace("-", "_")
		if key in IN_VALUES:
			return "IN"
		if key in OUT_VALUES:
			return "OUT"
	return None


def parse_bool(value, default=True):
	if value is None:
		return default
	if isinstance(value, bool):
		return value
	if isinstance(value, (int, float)):
		return bool(value)
	return str(value).strip().lower() not in {"0", "false", "no", "failed", "failure"}


def parse_record_number(punch):
	for key in ("record_number", "punching_record_number", "punching_rec_no", "nPunchingRecNo", "nRecNo"):
		value = normalize_text(punch.get(key))
		if value:
			return value
	return ""


def parse_event_id(punch):
	for key in ("event_id", "nEventID"):
		value = normalize_text(punch.get(key))
		if value:
			return value
	return ""


def parse_punch_time(value):
	if isinstance(value, datetime):
		return value.replace(tzinfo=None)
	if not value:
		return None
	text = str(value).strip()
	if text.endswith("Z"):
		text = text[:-1] + "+00:00"
	try:
		parsed = datetime.fromisoformat(text)
	except ValueError:
		for fmt in ("%Y-%m-%d %H:%M:%S", "%d-%m-%Y %H:%M:%S", "%Y/%m/%d %H:%M:%S"):
			try:
				parsed = datetime.strptime(text, fmt)
				break
			except ValueError:
				parsed = None
		if parsed is None:
			return None
	if parsed.tzinfo:
		parsed = parsed.astimezone().replace(tzinfo=None)
	return parsed


def build_stable_key(device_serial, user_id, punch_time, direction=None, record_number=None, event_id=None):
	device_serial = normalize_text(device_serial)
	record_number = normalize_text(record_number)
	event_id = normalize_text(event_id)
	if record_number:
		return f"{device_serial}:record:{record_number}"
	if event_id:
		return f"{device_serial}:event:{event_id}"

	raw = "|".join(
		[
			device_serial,
			normalize_text(user_id),
			punch_time.isoformat(sep=" ") if isinstance(punch_time, datetime) else normalize_text(punch_time),
			normalize_text(direction).upper(),
		]
	)
	return f"{device_serial}:fallback:{hashlib.sha256(raw.encode('utf-8')).hexdigest()[:24]}"


def normalize_punch(device_id, punch):
	device_serial = normalize_text(punch.get("device_serial") or punch.get("sn") or punch.get("szSN") or device_id)
	user_id = normalize_text(punch.get("user_id") or punch.get("szUserID"))
	card_no = normalize_text(punch.get("card_no") or punch.get("card_number") or punch.get("szCardNo"))
	direction = normalize_text(punch.get("direction") or punch.get("emDirection"))
	attendance_state = normalize_text(punch.get("attendance_state") or punch.get("emAttendanceState"))
	punch_time = parse_punch_time(punch.get("punch_time") or punch.get("time") or punch.get("timestamp") or punch.get("UTC"))
	record_number = parse_record_number(punch)
	event_id = parse_event_id(punch)
	log_type = normalize_log_type(direction, attendance_state)

	return {
		"stable_key": build_stable_key(device_serial, user_id, punch_time, direction, record_number, event_id),
		"device_serial": device_serial,
		"record_number": record_number,
		"event_id": event_id,
		"user_id": user_id,
		"card_no": card_no,
		"punch_time": punch_time,
		"direction": direction,
		"log_type": log_type,
		"attendance_state": attendance_state,
		"status": parse_bool(punch.get("status") if "status" in punch else punch.get("bStatus"), default=True),
		"open_method": normalize_text(punch.get("open_method") or punch.get("emOpenMethod") or punch.get("emMethod")),
		"error_code": normalize_text(punch.get("error_code") or punch.get("nErrorCode")),
		"raw": punch,
	}
