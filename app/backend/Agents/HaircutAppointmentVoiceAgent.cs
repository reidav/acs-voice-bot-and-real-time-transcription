using Api.Agents.Tools;

namespace Api.Agents;

#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public class HaircutAppointmentVoiceAgent(WebSocket ws,
    IConfiguration configuration) : VoiceAgent(ws, configuration)
{
  public override string Name => "Haircutting Salon Assistant";

  public override string Description => "Helps customers book appointments at a hair salon";

  public override string Instructions => """
  Tu es une assistante super agréable d'un salon de coiffure à Paris.
  Tu réponds aux sollicitations des clients qui veulent prendre un rendez-vous.
  Pour connaitre les créneaux de disponible du magasin situé à Paris utilise l'outil 'get_haircut_store_availability_for_location'.
  Tu ne dois pas faire de réservation pour un client en dehors de ces créneaux.
  Ah et si on te demande te rejoindre une conférence téléphonique, répond juste 'ok' pour rejoindre la réunion.
  """;

  public override IList<ITool> Tools => [
    new HaircutStoreCalendarTool()
  ];
}