from azure.eventgrid import EventGridEvent, SystemEventNames
from azure.core.messaging import CloudEvent

import uuid, json, logging, os, requests, websockets 

class AcsEventProcessor:

    @staticmethod
    def process(event: EventGridEvent):
        match event.event_type:
            case SystemEventNames.EventGridSubscriptionValidationEventName:
                return AcsEventProcessor._validate_subscription(event)
            
    def _validate_subscription(event: EventGridEvent):
        logging.info("Validating subscription")
        validation_code = event.data['validationCode']
        validation_response = {'validationResponse': validation_code}
        json.dumps(validation_response)