// Use unique namespaces for your apps if you going to share with others to avoid
// conflicting names
namespace HassModel;
/// <summary>
///     Lightmanager by the new HassModel API
/// </summary>

[NetDaemonApp]
public class LightManager
{
    public InputSelectEntity? HouseModeSelect { get; set; }
    private readonly Entities _entities;

    private readonly INetDaemonScheduler _scheduler;
    private readonly TimeSpan FLOWERLIGHTS_ON = TimeSpan.Parse("09:00:00");
    private readonly TimeSpan FLOWERLIGHTS_OFF = TimeSpan.Parse("21:00:00");
    private readonly TimeSpan BEDROOMLIGHTS_OFF = TimeSpan.Parse("20:30:00");
    //private readonly INetDaemonScheduler _scheduler;
    private readonly Services _services;

    //_scheduler RunDaily("23:00:00", () => DoSomeWork());
    public LightManager(IHaContext ha, INetDaemonScheduler scheduler)
    {
        _entities = new Entities(ha);
        _scheduler = scheduler;
        _services = new Services(ha);
        InitializeMorningScene();
        InitializeFlowerScene();
        InitializeBedtime();
        //_entities.Switch.HallenBlomlampa.TurnOff();

        _entities.InputSelect.Housestate.StateChanges()
            .Subscribe(
                n =>
                {
                    switch (n?.New?.State)
                    {
                        case "Morgon":
                            TurnOnAmbientLights();
                            break;
                        case "Dag":
                            TurnOffAmbientLights();
                            break;
                        case "Natt":
                            TurnOffAmbientLights();

                            break;
                        case "Kväll":
                            TurnOnAmbientLights();
                            _entities.Light.SovrummetFonster.TurnOn();
                            break;
                    }
                }
            );
    }

    /// <summary>
    ///     Returns true if it is currently night
    /// </summary>  
    public bool IsNight => HouseModeSelect?.State == "Natt";

    /// <summary>
    ///     Setup special scenes
    /// </summary>
    /// 
    private void InitializeBedtime()
    {
        _scheduler.RunDaily(BEDROOMLIGHTS_OFF, () => _entities.Light.SovrummetFonster.TurnOff());
    }
    private void InitializeFlowerScene()
    {
        _scheduler.RunDaily(FLOWERLIGHTS_ON, () => _entities.Switch.HallenBlomlampa.TurnOn());
        _scheduler.RunDaily(FLOWERLIGHTS_OFF, () => _entities.Switch.HallenBlomlampa.TurnOff());


        //scheduler.RunIn(TimeSpan.FromMinutes(5), ()=> test());
        // scheduler.RunEvery(TimeSpan.FromMinute(1), () => DoSomethingEveryMinute());
    }

    // private void test()
    // {
    //     _entities.Switch.HallenBlomlampa.TurnOff();
    // }

    private void InitializeMorningScene()
    {
        // Turns on lights when motion in kitchen and housestate is "natt" and between 5-9in morning
        _entities.BinarySensor.MotionKok
            .StateChanges()
            .Where(e => e.New?.State == "on")
            // .Where(e => IsNight)
            .Subscribe(s =>
                {
                    // If early morning time then turn on lights
                    if (IsTimeNowBetween(TimeSpan.FromHours(5), TimeSpan.FromHours(9)))
                    {
                        _entities.Light.Koket.TurnOn();

                    }

                });
        // Turn off after som time idle except if it is morning then keep on until daytime will turn off
        _entities.BinarySensor.MotionKok
            .StateChanges()
            .Where(e =>
                e.New.IsOff() &&
                e.Old.IsOn() &&
                IsNight &&
                !IsTimeNowBetween(TimeSpan.FromHours(5), TimeSpan.FromHours(10)))
            .Throttle(TimeSpan.FromMinutes(15))
            .Subscribe(s => _entities.Light.Koket.TurnOff(transition: 0));
    }

    private void TurnOffAmbientLights()
    {
        //_entities.Light.KontoretTak.TurnOff();
        //_entities.Light.Hall1.TurnOff();
        _entities.Light.KontoretFonster.TurnOff();
        _entities.Switch.KoketFonster.TurnOff();
        _entities.Switch.VardagsrummetFonsterTv.TurnOff();
        _entities.Light.VardagsrummetFonsterLilla.TurnOff();
        _entities.Light.GastrummetFonster.TurnOff();
    }

    private void TurnOnAmbientLights()
    {
        //_entities.Light.KontoretTak.TurnOn();
        //_entities.Light.Hall1.TurnOn();
        _entities.Light.KontoretFonster.TurnOn();
        _entities.Switch.KoketFonster.TurnOn();
        _entities.Switch.VardagsrummetFonsterTv.TurnOn();
        _entities.Light.VardagsrummetFonsterLilla.TurnOn();
        _entities.Light.GastrummetFonster.TurnOn();
    }
    /// <summary>
    ///     Check if time between 
    /// </summary>
    private static bool IsTimeNowBetween(TimeSpan fromSpan, TimeSpan toSpan)
    {
        var now = DateTime.Now.TimeOfDay;
        if (now >= fromSpan && now <= toSpan)
            return true;

        return false;
    }
}