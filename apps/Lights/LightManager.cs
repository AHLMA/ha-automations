// Use unique namespaces for your apps if you going to share with others to avoid
// conflicting names
namespace HassModel;
/// <summary>
///     Lightmanager by the new HassModel API
/// </summary>
[NetDaemonApp]
public class LightManager
{
    private readonly Entities _entities;

    public LightManager(IHaContext ha)
    {
        _entities = new Entities(ha);
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
                            break;
                    }
                }
            );
    }

    private void TurnOffAmbientLights()
    {
        //_entities.Light.KontoretTak.TurnOff();
        //_entities.Light.Hall1.TurnOff();
        _entities.Light.KontoretFonster.TurnOff();
        _entities.Switch.KoketFonster.TurnOff();
        _entities.Switch.VardagsrummetFonsterTv.TurnOff();
    }

    private void TurnOnAmbientLights()
    {
        //_entities.Light.KontoretTak.TurnOn();
        //_entities.Light.Hall1.TurnOn();
        _entities.Light.KontoretFonster.TurnOn();
        _entities.Switch.KoketFonster.TurnOn();
        _entities.Switch.VardagsrummetFonsterTv.TurnOn();
    }
}