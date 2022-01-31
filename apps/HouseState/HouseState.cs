[NetDaemonApp]
public class HouseStateManager
{
    private readonly Entities _entities;
    private readonly INetDaemonScheduler _scheduler;
    private readonly ILogger<HouseState> _log;

    private readonly TimeSpan NIGHTTIME_WEEKDAYS = TimeSpan.Parse("22:30:00");
    private readonly TimeSpan NIGHTTIME_WEEKENDS = TimeSpan.Parse("23:30:00");
    private readonly TimeSpan DAYTIME = TimeSpan.Parse("09:00:00");

    private double ElevationEvening => 6.0;
    private double ElevationMorning => -1.0;

    public HouseStateManager(IHaContext ha, INetDaemonScheduler scheduler, ILogger<HouseState> logger)
    {
        _entities = new Entities(ha);
        _scheduler = scheduler;
        _log = logger;

        SetDayTimeSchedule();
        SetNightTimeSchedule();
        SetEveningSchedule();
        SetMorningSchedule();

    }
    private readonly DayOfWeek[] WeekdayNightDays = new DayOfWeek[]
            {
            DayOfWeek.Sunday,
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            };

    private readonly DayOfWeek[] WeekendNightDays = new DayOfWeek[]
        {
            DayOfWeek.Friday,
            DayOfWeek.Saturday,
        };

    public bool IsDaytime => _entities.InputSelect.Housestate.State == "Dag";
    public bool IsNighttime => _entities.InputSelect.Housestate.State == "Natt";
    public bool IsCloudy => _entities.Weather.SmhiHome?.Attributes?.Cloudiness > 90.0;
    private void SetDayTimeSchedule()
    {
        _log.LogInformation($"Setting daytime: {DAYTIME}");
        _scheduler.RunDaily(DAYTIME, () => SetHouseState(HouseState.Day));
    }

    /// <summary>
    ///     When elevation <= 9 and sun is not rising and depending if
    ///     it is cloudy set the evening state
    /// </summary>
    private void SetEveningSchedule()
    {
        _entities.Sun.Sun
            .StateAllChanges()
            .Where(
                n =>
                    n?.New?.Attributes?.Elevation <= ElevationEvening &&
                    n?.New?.Attributes?.Rising == false &&
                    n?.Old?.Attributes?.Elevation > ElevationEvening
            )
            .Subscribe(
                s =>
                {
                    if (IsCloudy)
                    {
                        SetHouseState(HouseState.Evening);
                        _log.LogInformation("It is evening {time}", DateTime.Now);
                    }
                    else
                    {
                        _log.LogInformation("It is evening {time} not cloudy set evening in 45 minuts!", DateTime.Now);
                        _scheduler.RunIn(TimeSpan.FromMinutes(45),
                            () => SetHouseState(HouseState.Evening));
                    }
                }
            );
    }

    /// <summary>
    ///     When elevation <= 9 and sun is not rising and depending if
    ///     it is cloudy set the morning state
    /// </summary>
    private void SetMorningSchedule()
    {
        _entities.Sun.Sun
            .StateAllChanges()
            .Where(
                n =>
                    n?.New?.Attributes?.Elevation >= ElevationMorning &&
                    n?.New?.Attributes?.Rising == true &&
                    n?.Old?.Attributes?.Elevation < ElevationMorning
            )
            .Subscribe(
                s =>
                {
                    if (!IsCloudy)
                    {
                        SetHouseState(HouseState.Morning);
                        _log.LogInformation("It cloudy set morning {time}", DateTime.Now);
                    }
                    else
                    {
                        _log.LogInformation("It morning {time} and not cloudy set morning state in 45 minuts!", DateTime.Now);
                        _scheduler.RunIn(TimeSpan.FromMinutes(45),
                            () => SetHouseState(HouseState.Morning));
                    }
                }
            );
    }

    /// <summary>
    ///     Set night time schedule on different time different weekdays
    /// </summary>
    private void SetNightTimeSchedule()
    {
        _log.LogInformation($"Setting weekday night time to: {NIGHTTIME_WEEKDAYS}");
        _scheduler.RunDaily(NIGHTTIME_WEEKDAYS, () =>
        {
            if (WeekdayNightDays.Contains(DateTime.Now.DayOfWeek))
                SetHouseState(HouseState.Night);
        });

        _log.LogInformation($"Setting weekend night time to: {NIGHTTIME_WEEKENDS}");

        _scheduler.RunDaily(NIGHTTIME_WEEKENDS, () =>
        {
            if (WeekendNightDays.Contains(DateTime.Now.DayOfWeek))
                SetHouseState(HouseState.Night);
        });
    }


    /// <summary>
    ///     Sets the house state to specified state and updates Home Assistant InputSelect
    /// </summary>
    /// <param name="state">State to set</param>
    private void SetHouseState(HouseState state)
    {
        _log.LogInformation($"Setting current house state to {state}", state);
        var select_state = state switch
        {
            HouseState.Morning => "Morgon",
            HouseState.Day => "Dag",
            HouseState.Evening => "Kväll",
            HouseState.Night => "Natt",
            HouseState.Cleaning => "Städning",
            _ => throw new ArgumentException("Not supported", nameof(state))
        };
        _entities.InputSelect.Housestate.SelectOption(option: select_state);
    }
}

public enum HouseState
{
    Morning,
    Day,
    Evening,
    Night,
    Cleaning,
    Unknown
}