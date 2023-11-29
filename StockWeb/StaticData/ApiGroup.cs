namespace StockWeb.StaticData
{
    public static class ApiGroups
    {
        public static readonly ApiGroup Account = new ApiGroup("登入相關", "登入使用JWT機制", "v1");
        public static readonly ApiGroup Stock = new ApiGroup("股票相關", "股票資訊從櫃買中心、證交所、公開資訊觀測站取得", "v1");
    }
    public class ApiGroup
    {
        public string DownListName { get; }
        public string Description { get; }
        public string Version { get; }

        public ApiGroup(string downListName, string description, string version)
        {
            DownListName = downListName;
            Description = description;
            Version = version;
        }
    }

}
