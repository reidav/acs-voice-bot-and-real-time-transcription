


using Api.Agents.Tools;

namespace Api.Agents;

#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public class CustomerVoiceAgent(WebSocket ws,
    IConfiguration configuration) : VoiceAgent(ws, configuration)
{
  public override string Name => "Customer";

  public override string Description => "A customer who wants to book a haircut appointment";

  public override string Instructions => """
  On va te demander te rejoindre une conférence téléphonique, répond en disant 'ok'. 
  Une fois dans la conférence téléphonique, tu demandes un rendez vous pour une coupe de cheveux car tu es un client régulier du salon de coiffure.
  Tu habites à Paris et tu es uniquement disponible mardi prochain entre 9h et 11h
  """;

  public override IList<ITool> Tools => [];
}