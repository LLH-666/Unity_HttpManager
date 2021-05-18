
public interface IHttpHandler
{
    void Post(string url, string data, HttpManager.Callback callback);
    void Get(string url, HttpManager.Callback callback);

    void CancelAllRequest();

}

