using NetDaemon.Daemon;

// [Focus]
[NetDaemonApp]
public class LottasHomeComingApp
{
    private readonly Entities _entities;
    public LottasHomeComingApp(IHaContext ha, INetDaemonScheduler scheduler, ILogger<LottasHomeComingApp> logger, ITextToSpeechService tts)
    {
        _entities = new Entities(ha);

        _entities.BinarySensor.MotionHall
            .StateChanges()
            .Where(n => n.New.IsOn())
            .Subscribe(
                _ => tts.Speak("media_player.hallen", "Välkommen lotta, kul att du kommer! vi har längtat efter dig lotta. Så tråkigt vi har haft utan dig!")
            );
    }
}