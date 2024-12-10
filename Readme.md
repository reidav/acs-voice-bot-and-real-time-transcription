# ACS Voice Bot & Real Time Transcription

ACS Voice Bot & Real Time Transcription is a sample application that demonstrates how to use Azure Communication Services (ACS) to create a voice bot that can transcribe real-time audio. 


## Prerequisites

- An Azure subscription. If you don't have an Azure subscription, create a [free account](https://azure.microsoft.com/free/) before you begin.
- An Azure Communication Services resource. Create an [Azure Communication Services](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource) resource before you begin.
- WIP ...

## Setup Instructions – Local environment  

#### 1. Setup and host your Azure DevTunnel
[Azure DevTunnels](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/overview) is an Azure service that enables you to share local web services hosted on the internet. Use the commands below to connect your local development environment to the public internet. This creates a tunnel with a persistent endpoint URL and which allows anonymous access. We will then use this endpoint to notify your application of chat and job router events from the Azure Communication Service.
```bash
devtunnel host -p 8000 -allow-anonymous
```
Make a note of the devtunnel URI. You will need it at later steps.


