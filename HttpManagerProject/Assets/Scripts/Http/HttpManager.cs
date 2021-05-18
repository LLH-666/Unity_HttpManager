using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System;
using System.IO;
using UnityEngine.SocialPlatforms;

public class HttpManager : MonoBehaviour
{
    //文本类型
    public const string CONTENT_TYPE = "Content-type";
    public const string JSON_TYPE = "application/json; charset='utf-8'";

    //编码
    public const string ACCEPT_ENCODING = "Accept-Encoding";
    public const string GZIP = "gzip";
    public const string AGENT = "FGame";

    //回调结果是空
    public const string EMPTY = "";
    //回调错误信息：没有联网
    public const string ERROR_NOT_REACHABLE = "error:not_reachable";
    //回调错误信息：url不合法
    public const string ERROR_INVALID_URL = "error:invalid_url";

    //hack 上传地址
    public const string ADDRESS = "";
    public const string UPDATE_CONFIG_INTERFACE = "";
    public const string GET_CONFIG_INTERFACE = "";
    public const string REPORT_INTERFACE = "";

    //回调
    public delegate void Callback(string result, long code, string error);

    //单例
    private static HttpManager instance = null;
    public static HttpManager GetInstance()
    {
        if (instance == null)
        {
            GameObject obj = new GameObject("HttpManager");
            DontDestroyOnLoad(obj);
            instance = obj.AddComponent<HttpManager>();
        }
        return instance;
    }

    private IHttpHandler httpHandler;

    private void Awake()
    {
        httpHandler = new BestHttpHandler(this);
        
        //MyTest测试
        // DebugUtils.Log($"https://word.socialgameapp.com/：{Descryp1.Decode(HttpManager.ADDRESS, HttpManager.urlKey)}");
        // DebugUtils.Log($"wordconnect/v1/update_config：{Descryp1.Decode(HttpManager.UPDATE_CONFIG_INTERFACE, HttpManager.urlKey)}");
        // DebugUtils.Log($"wordconnect/v1/get_config：{Descryp1.Decode(HttpManager.GET_CONFIG_INTERFACE, HttpManager.urlKey)}");
        // DebugUtils.Log($"wordconnect/v1/report_id：{Descryp1.Decode(HttpManager.REPORT_INTERFACE, HttpManager.urlKey)}");
    }

    private void OnDestroy()
    {
        instance = null;
    }

    public void Post(string actionName, Dictionary<string, object> data, Callback callback)
    {
        if (gameObject == null)
        {
            return;
        }

        actionName = GetUrl(actionName, data);
        httpHandler.Post(actionName, Utilities.ConvertToJsonString(data), callback);
    }

    public void Get(string url, Callback callback)
    {
        httpHandler.Get(url, callback);
    }

    public void CancelAllRequest()
    {
        httpHandler.CancelAllRequest();
    }

    public void Dispose()
    {
        DestroyImmediate(gameObject);
    }

    /*例子
        Dictionary<string, object> dataJson = new Dictionary<string, object>();
        dataJson.Add("game_user_id", "test");
        string url = HttpManager.GetInstance().GetUrl(HttpManager.UPDATE_CONFIG_INTERFACE , dataJson);
        DebugUtils.Log(url);
    */
    /// <summary>
    /// 获得要Get或Post的地址
    /// </summary>
    /// <param name="interfaceName">接口名字，详见const定义</param>
    /// <param name="dataJson">除固定参数外的参数</param>
    /// <returns></returns>
    public string GetUrl(string interfaceName, Dictionary<string, object> dataJson = null)
    {
        if (dataJson == null)
        {
            dataJson = new Dictionary<string, object>();
        }
        //获得平台、版本号、语言、是否是真实环境
        dataJson.Add("ua", GetUA()); 
        //获得有效时间
        dataJson.Add("expire", GetExpire());
        //获得随机字符
        dataJson.Add("nonce", GetNonce());
        //获取到Token Token解释： https://www.jianshu.com/p/24825a2683e6
        string token = GetToken(dataJson);
        dataJson.Add("token", token);
        //获取url
        string url = ADDRESS + interfaceName + '?';
        int index = 0;
        foreach (KeyValuePair<string, object> item in dataJson)
        {
            url += item.Key;
            url += '=';
            url += item.Value;
            url += '&';
            index++;
        }
        url = url.Remove(url.Length - 1);
        return url;
    }

    /// <summary>
    /// 获取Token
    /// </summary>
    /// <param name="dataJson"></param>
    /// <returns></returns>
    private string GetToken(Dictionary<string, object> dataJson)
    {
        string[] names = new string[dataJson.Count];
        int index = 0;
        foreach (KeyValuePair<string, object> item in dataJson)
        {
            names[index] = item.Key;
            index++;
        }
        for (int i = 0; i < names.Length - 1; i++)
        {
            int minIndex = i;
            for (int j = i + 1; j < names.Length; j++)
            {
                //Unicode码不是ASCII码，不知道会不会有问题
                int result = string.CompareOrdinal(names[minIndex], names[j]);//string.Compare(names[minIndex], names[j]);
                if (result == 1)
                {
                    minIndex = j;
                }
            }
            string temp = names[minIndex];
            names[minIndex] = names[i];
            names[i] = temp;
        }

        string token = "";
        foreach (string key in names)
        {
            token += key;
            token += dataJson[key].ToString();
        }

        token += "47LwDfWpcVZypl7f";
        Debug.Log("token:" + token);
        string md5 = GetMd5(token);
        Debug.Log("md5:" + md5.Substring(6, 16));
        return md5.Substring(8, 16);
    }
    
    /// <summary>
    /// 获取Md5
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private string GetMd5(string input)
    {
        // 创建 MD5CryptoServiceProvider 对象的新实例。
        MD5 md5Hasher = MD5.Create();
        
        // 将输入字符串转换为字节数组并计算哈希。
        byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
        
        // 创建一个新的 StringBuilder 来收集字节并创建一个字符串。
        StringBuilder sBuilder = new StringBuilder();
        
        // 循环遍历散列数据的每个字节，并将每个字节格式化为十六进制字符串。
        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }
        
        // 返回十六进制字符串。
        return sBuilder.ToString();
    }

    /// <summary>
    /// 得到随机字符
    /// </summary>
    /// <returns></returns>
    private string GetNonce() 
    {
        int count = UnityEngine.Random.Range(5, 10);
        string str = string.Empty;
        long num2 = DateTime.Now.Ticks;
        System.Random random = new System.Random(((int)(((ulong)num2) & 0xffffffffL)) | ((int)(num2 >> count)));
        for (int i = 0; i < count; i++)
        {
            char ch;
            int num = random.Next();
            if ((num % 2) == 0)
            {
                ch = (char)(0x30 + ((ushort)(num % 10)));
            }
            else
            {
                ch = (char)(0x41 + ((ushort)(num % 0x1a)));
            }
            str = str + ch.ToString();
        }
        return str;
    }

    /// <summary>
    /// 得到有效时间（当前时间+10s）
    /// </summary>
    /// <returns></returns>
    private long GetExpire()
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        long nowSec = Convert.ToInt64(ts.TotalSeconds);
        return nowSec + 10;
    }

    /// <summary>
    /// 得到平台、版本号、语言、是否是真实环境
    /// </summary>
    /// <returns></returns>
    private string GetUA()
    {
        string ua = "";
        ua += Application.platform.ToString();
        ua += '-';
        ua += Application.version.ToString();
        ua += '-';
        ua += Application.systemLanguage.ToString();
        ua += '-';
        ua += "release";
        return ua;
    }
}

