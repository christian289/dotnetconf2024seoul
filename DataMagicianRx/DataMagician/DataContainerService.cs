namespace DataMagician;

internal class DataContainerService
{
    public DataContainerService()
    {
        RawDataSubject = new Subject<string>();
    }

    public Subject<string> RawDataSubject { get; init; }
}