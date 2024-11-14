import threading
from typing import List

class Call:
    def __init__(self, call_id: str, call_type: str, call_status: str):
        self.call_id = call_id
        self.call_type = call_type
        self.call_status = call_status

    def to_dict(self):
        return {
            "call_id": self.call_id,
            "call_type": self.call_type,
            "call_status": self.call_status,
        }

class CallManager:
    lock = threading.Lock()
    
    def __init__(self):
        self.calls: List[Call] = []

    def add_call(self, call: Call):
        with self.lock:
            self.calls.append(call)
    
    def get_call(self, call_id: str):
        return self.calls.filter(lambda call: call.call_id == call_id)
    
    def update_call(self, call_id: str, call_status: str):
        with self.lock:
            for call in self.calls:
                if call.call_id == call_id:
                    call.call_status = call_status
                    return call
        return None

    def to_dict(self):
        return {
            "call_id": self.call_id,
            "call_type": self.call_type,
            "call_status": self.call_status,
        }