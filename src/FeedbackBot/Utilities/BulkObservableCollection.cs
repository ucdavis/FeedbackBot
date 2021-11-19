using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FeedbackBot.Utilities;

/// <summary>
/// Adds AddRange method to ObservableCollection.
/// </summary>
/// <typeparam name="T"></typeparam>
public class BulkObservableCollection<T> : ObservableCollection<T>
{
    public BulkObservableCollection() : base() { }

    public BulkObservableCollection(IEnumerable<T> collection) : base(collection) { }

    public BulkObservableCollection(List<T> list) : base(list) { }

    public void AddRange(IEnumerable<T> range)
    {
        var startIndex = this.Count;

        foreach (var item in range)
        {
            Items.Add(item);
        }

        this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, range, startIndex));
    }

    public void Reset(IEnumerable<T> range)
    {
        this.Items.Clear();

        AddRange(range);
    }
}
