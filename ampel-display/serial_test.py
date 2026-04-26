"""Quick serial test - run WITHOUT the matrix to confirm data arrives clean."""
import serial
import json
import sys
import time

port = sys.argv[1] if len(sys.argv) > 1 else "/dev/ttyUSB0"
baud = int(sys.argv[2]) if len(sys.argv) > 2 else 115200
ser = serial.Serial(port, baud, timeout=1)
print(f"Listening on {port} at {baud} baud...")
print("Make sure the C# app is broadcasting. Press Ctrl+C to stop.\n")

good = 0
bad = 0
start = time.time()

try:
    while time.time() - start < 15:
        line = ser.readline()
        if line:
            text = line.decode("utf-8", errors="ignore").strip()
            if not text:
                continue
            try:
                data = json.loads(text)
                good += 1
                print(f"  GOOD #{good}: {text[:80]}")
            except json.JSONDecodeError:
                bad += 1
                print(f"  BAD  #{bad}: {repr(text[:80])}")
except KeyboardInterrupt:
    pass

ser.close()
print(f"\nResult: {good} good, {bad} bad out of {good+bad} messages")
if bad > 0 and good == 0:
    print("=> All garbled: likely baud rate mismatch or wiring issue")
elif bad > 0:
    print("=> Some garbled: likely electrical noise or timing issue")
else:
    print("=> All clean! The matrix DMA is the problem when both run together.")
