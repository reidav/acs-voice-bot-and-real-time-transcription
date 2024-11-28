#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


using Newtonsoft.Json;

namespace Api.Agents.Tools;

public class HaircutStoreCalendarToolInput
{
    public string? City { get; set; }
}

public class HaircutStoreCalendarTool : Tool
{
    public HaircutStoreCalendarTool() {}

    public override ConversationFunctionTool Definition => new()
    {
        Name = "get_haircut_store_availability_for_location",
        Description = "gets the availability for a location",
        Parameters = BinaryData.FromString(@"
                {
                  ""type"": ""object"",
                  ""properties"": {
                    ""city"": {
                      ""type"": ""string"",
                      ""description"": ""The city where the haircut store is located""
                    }
                  },
                  ""required"": [""city""],
                  ""additionalProperties"": false
                }
        ")
    };

    public override Task<string> ExecuteAsync(string jsonParams)
    {
        var input = JsonConvert.DeserializeObject<HaircutStoreCalendarToolInput>(jsonParams);

        if (input == null || string.IsNullOrEmpty(input.City))
        {
            throw new Exception("Invalid input");
        }
        
        switch (input.City.ToLower().Trim())
        {
            case "paris":
                return Task.FromResult("The haircut store in Paris is open from 9am to 6pm, Monday and Tuesday only.");
            default:
                return Task.FromResult("There's no haircut store in this city, so we have no availability");
        }
    }
}