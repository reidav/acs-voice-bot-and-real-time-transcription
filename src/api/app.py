from fastapi import FastAPI, Request, Response, WebSocketDisconnect, status, WebSocket, websockets
from dotenv import load_dotenv
import uuid, json, logging, os, requests, websockets 
from urllib.parse import urlencode, urljoin

from azure.eventgrid import EventGridEvent, SystemEventNames
from azure.core.messaging import CloudEvent
from azure.communication.callautomation import (
    CallAutomationClient,
    TranscriptionTransportType,
    TranscriptionOptions,
    MediaStreamingOptions,
    MediaStreamingAudioChannelType,
    MediaStreamingContentType
    )

# from call_state_provider import CallState, CallStateProvider

logging.basicConfig(level=logging.INFO)
app = FastAPI()

load_dotenv()

ACS_CONNECTION_STRING = os.getenv('ACS_CONNECTION_STRING')
COGNITIVE_SERVICE_ENDPOINT = os.getenv('COGNITIVE_SERVICE_ENDPOINT')
LOCALE = os.getenv('LOCALE')
TRANSPORT_URL = os.getenv('TRANSPORT_URL')
CALLBACK_URI_HOST = os.getenv('CALLBACK_URI_HOST')
CALLBACK_EVENTS_URI = CALLBACK_URI_HOST + "/api/events/callbacks"

call_automation_client = CallAutomationClient.from_connection_string(
    ACS_CONNECTION_STRING
)

@app.post("/api/events/incoming-call")
async def handle_incoming_call(request: Request):
    logging.info("Incoming call triggered ...")

    for event_dict in await request.json():
        event = EventGridEvent.from_dict(event_dict)
        logging.info("Incoming event data --> %s", event.data)

        # Validate the subscription
        if event.event_type == SystemEventNames.EventGridSubscriptionValidationEventName:
            logging.info("Validating subscription")
            validation_code = event.data['validationCode']
            validation_response = {'validationResponse': validation_code}
            return Response(content=json.dumps(validation_response), status_code=status.HTTP_200_OK)

        # Handle incoming call
        elif event.event_type =="Microsoft.Communication.IncomingCall":
            logging.info("Incoming call received: data=%s", event.data)  

            # Get the caller id
            if event.data['from']['kind'] =="phoneNumber":
                caller_id =  event.data['from']["phoneNumber"]["value"]
            else :
                caller_id =  event.data['from']['rawId'] 
            logging.info("incoming call handler caller id: %s", caller_id)

            # Set the callback uri with the caller id
            incoming_call_context=event.data['incomingCallContext']
            guid = uuid.uuid4()
            query_parameters = urlencode({"callerId": caller_id})
            callback_uri = f"{CALLBACK_EVENTS_URI}/{guid}?{query_parameters}"

            logging.info("callback url: %s",  callback_uri)
            logging.info("transport url: %s",  TRANSPORT_URL)
            transcription_configuration=TranscriptionOptions(
                        transport_url=TRANSPORT_URL,
                        transport_type=TranscriptionTransportType.WEBSOCKET,
                        locale=LOCALE,
                        start_transcription=False
                        )
            
            media_streaming_options = MediaStreamingOptions(
                transport_url=TRANSPORT_URL,
                transport_type=TranscriptionTransportType.WEBSOCKET,
                content_type=MediaStreamingContentType.AUDIO,
                audio_channel_type=MediaStreamingAudioChannelType.MIXED,
                start_media_streaming=True
            )

            answer_call_result = call_automation_client.answer_call(
                incoming_call_context=incoming_call_context,
                transcription=transcription_configuration,
                cognitive_services_endpoint=COGNITIVE_SERVICE_ENDPOINT,
                callback_url=callback_uri,
                media_streaming=media_streaming_options
            )
            logging.info("Answered call for connection id: %s", answer_call_result.call_connection_id)
            return Response(status_code=status.HTTP_200_OK)

    return Response(status_code=status.HTTP_200_OK)


@app.post("/api/events/callbacks/{context_id}")
async def callback_events_handler(request: Request):
    logging.info("Callback triggered ...")
    try:
        for event_dict in await request.json():
            event = CloudEvent.from_dict(event_dict)
            logging.info(f'Event : {event.data}')

            call_connection_id = event.data['callConnectionId']
            logging.info(f'call connection id: {call_connection_id}, event type: {event.type}')

            caller_id = request.query_params["callerId"].strip()
            if "+" not in caller_id:
                caller_id="+".strip()+caller_id.strip()
            logging.info(f'caller id: {caller_id}')

            if event.type == "Microsoft.Communication.CallDisconnected":
                logging.info("Call disconnected event")

                logging.info("Stopping transcription")
                callconnection = call_automation_client.get_call_connection(call_connection_id)
                callconnection.stop_media_streaming()
                # callconnection.stop_transcription()
                logging.info("Transcription stopped")

                return Response(status_code=200)
            
            elif event.type == "Microsoft.Communication.CallConnected":
                logging.info("Call connected event")

                # Initiate transcription
                logging.info("Initiating transcription")
                callconnection = call_automation_client.get_call_connection(call_connection_id)
                callconnection.start_media_streaming()
                # callconnection.start_transcription(locale=LOCALE, operation_context="StartTranscript")
                logging.info("Transcription started")

            logging.info(f"call connected : data={event.data}")
        return Response(status_code=200)
    except Exception as ex:
        logging.info("error in event handling --> " + str(ex))

@app.get('/api/download/{content_location}')
async def download_recording(content_location: str, request: Request):
    try:
        logging.info("Content location : %s", content_location)
        recording_data = call_automation_client.download_recording(content_location)
        with open("Recording_File.wav", "wb") as binary_file:
            binary_file.write(recording_data.read())
        return Response(response="Ok")
    except Exception as ex:
        logging.info("Failed to download recording --> " + str(ex))
        return Response(text=str(ex), status_code=500)
        
@app.post("/api/events/recording-file-status")
async def recording_file_status(request: Request):
    try:
        for event_dict in await request.json():
            event = EventGridEvent.from_dict(event_dict)
            if event.event_type ==  SystemEventNames.EventGridSubscriptionValidationEventName:
                code = event.data['validationCode']
                if code:
                    data = {"validationResponse": code}
                    logging.info("Successfully Subscribed EventGrid.ValidationEvent --> " + str(data))
                    return Response(content=str(data), status_code=200)

            if event.event_type == SystemEventNames.AcsRecordingFileStatusUpdatedEventName:
                acs_recording_file_status_updated_event_data = event.data
                acs_recording_chunk_info_properties = acs_recording_file_status_updated_event_data['recordingStorageInfo']['recordingChunks'][0]
                # logging.info("acsRecordingChunkInfoProperties response data --> " + str(acs_recording_chunk_info_properties))
                content_location = acs_recording_chunk_info_properties['contentLocation']
                logging.info("contentLocation --> " + str(content_location))
                return Response(content="Ok")
                                                  
    except Exception as ex:
         logging.error( "Failed to get recording file")
         return Response(content='Failed to get recording file', status_code=400)

@app.websocket("/api/events/transcript")
async def websocket_endpoint(websocket: WebSocket):
    await websocket.accept()
    try:
        while True:
            data = await websocket.receive_text()
            logging.info(f"Data received: {data}")
    except WebSocketDisconnect:
        logging.info("Websocket disconnected")

def initiate_transcription(call_connection_id):
    logging.info("initiate_transcription is called %s", call_connection_id)
    callconnection = call_automation_client.get_call_connection(call_connection_id)
    callconnection.start_transcription(locale=LOCALE, operation_context="StartTranscript")
    logging.info("Starting the transcription")