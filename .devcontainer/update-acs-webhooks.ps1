# Variables
$subscriptionId = ""
$resourceGroupName = ""
$acsResourceName = ""
$eventSubscriptionName = "EventsWebhookSubscription"
$endpoint = "https://<endpoint>.devtunnels.ms/api/events"
$includedEventTypes = "Microsoft.Communication.IncomingCall"
$sourceResourceId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Communication/CommunicationServices/$acsResourceName"

# Register the EventGrid provider
az provider register --namespace Microsoft.EventGrid

# Check if the event subscription exists
$eventSubscriptionExists = az eventgrid event-subscription list --source-resource-id $sourceResourceId --query "[?name=='$eventSubscriptionName']" --output tsv

if (-not $eventSubscriptionExists) {
    # Create the event subscription if it does not exist
    az eventgrid event-subscription create `
        --name $eventSubscriptionName `
        --source-resource-id $sourceResourceId `
        --included-event-types $includedEventTypes `
        --endpoint-type webhook `
        --endpoint $endpoint
} else {
    # Update the event subscription if it exists
    az eventgrid event-subscription update `
        --name $eventSubscriptionName `
        --source-resource-id $sourceResourceId `
        --included-event-types $includedEventTypes `
        --endpoint-type webhook `
        --endpoint $endpoint
}
