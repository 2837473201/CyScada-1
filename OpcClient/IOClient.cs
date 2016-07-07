namespace CyScada.Web.OpcClient
{
    public delegate void LogHandler(string LogString);
    public delegate void RecHandler(object[,] rec);

    public interface IOClient
    {
        string ItemConfigPath { get; set; }
        string ServerAddress { get; set; }
        bool AddItem(string ItemName, string ProxyString);
        void AddItems(string[] ItemNames, string[] ProxyStrings);
        void AddItems(string[] ItemNames);
        bool AddItem(string ItemName);
        bool ConnectServer();
        void DisConnectServer();
        string ErrorMessage { get; }
        void RemoveItem(string ItemName);
        void RemoveItem(string[] ItemNames);
        void StartRead();
        void StopRead();
        string ServerStatus { get; }
        string ServerType { get; }
        void WriteValue(string ItemName, string ItemValueType, object value);
        void SetProxyFormat(string ProxyFormat);
        event LogHandler EnqueueLog;
        event RecHandler EnqueueRec;
    }
}
