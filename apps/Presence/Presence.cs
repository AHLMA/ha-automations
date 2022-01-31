using NetDaemon.Daemon;

// [Focus]
[NetDaemonApp]
public class Presence
{
    private readonly Entities _entities;
    private readonly Services _services;

    public Presence(IHaContext ha)
    {
        _entities = new Entities(ha);
        _services = new Services(ha);
        // _services.Tts.CloudSay("media_player.koket", message: "Hej");
        // _services.Tts.CloudSay()
        //ha.CallService("tts", "cloud_say", ServiceTarget.FromEntities("media_player.koket"), new { message = "Hej" });
    }

}