using DynamicData;
using DynamicData.Binding;
using ReactiveApp.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace ReactiveApp.ViewModels;
public class MainViewModel : ReactiveObject, IActivatableViewModel
{
    private ReadOnlyObservableCollection<WeatherForecastViewModel> items;

    public ReadOnlyObservableCollection<WeatherForecastViewModel> Items => items;

    // SourceCache<> has performance benefits over SourceList<>. 
    // Use it when an identifier exist for each item.
    protected SourceList<WeatherForecastViewModel> ItemsSourceList { get; } = new();

    [Reactive] public double Average { get; private set; }

    [Reactive] public string SearchTerm { get; set; }

    [Reactive] public SortDirection SortDirection { get; set; }

    public ViewModelActivator Activator { get; } = new ViewModelActivator();

    public MainViewModel()
    {
        this.WhenActivated(disposables =>
        {
            var observableFilter = this
                .WhenAnyValue(viewModel => viewModel.SearchTerm)
                .Throttle(TimeSpan.FromMilliseconds(250))
                .Select(MakeFilter);

            var observableSort = ItemsSourceList.Connect()
                .WhenValueChanged(x => x.TemperatureC)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Select(_ => MakeSort());

            var observableSortAscending = this
                .WhenAnyValue(viewModel => viewModel.SortDirection)
                .Select(_ => MakeSort());

            ItemsSourceList.Connect()
                .Filter(observableFilter)
                .Sort(observableSort.Merge(observableSortAscending))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out items)
                .Subscribe()
                .DisposeWith(disposables);

            // Track changes of TemperatureC and calculate the average
            Items.ToObservableChangeSet()
                .WhenValueChanged(x => x.TemperatureC)
                .Subscribe(_ => Average = Items.Any() ? Items.Average(x => x.TemperatureC) : 0)
                .DisposeWith(disposables);

            this.RaisePropertyChanged(nameof(Items));
            LoadData();
        });
    }

    private async void LoadData()
    {
        var weathers = await WeatherForecast.GetWeatherForecasts();

        ItemsSourceList.AddRange(weathers!.Select(x => new WeatherForecastViewModel
        {
            Date = x.Date,
            TemperatureC = x.TemperatureC,
            Summary = x.Summary
        }));
    }

    private Func<WeatherForecastViewModel, bool> MakeFilter(string term)
    {
        return x => string.IsNullOrEmpty(term) ||
        x.Summary!.Contains(term, StringComparison.InvariantCultureIgnoreCase);
    }

    private SortExpressionComparer<WeatherForecastViewModel> MakeSort()
    {
        return SortDirection == SortDirection.Ascending ?
            SortExpressionComparer<WeatherForecastViewModel>.Ascending(t => t.TemperatureC) :
            SortExpressionComparer<WeatherForecastViewModel>.Descending(t => t.TemperatureC);
    }
}

public class WeatherForecastViewModel : ReactiveObject
{
    [Reactive] public DateOnly Date { get; set; }
    [Reactive] public int TemperatureC { get; set; }
    [Reactive] public string? Summary { get; set; }
}
