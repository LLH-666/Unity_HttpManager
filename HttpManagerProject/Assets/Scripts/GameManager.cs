using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private string deviceId = "window";
    private JSONNode userInfo = null;
    
    private int lastCompletedLevelNumber = 0;
    private int coins = 0;

    private void Start()
    {
        LoadData();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            lastCompletedLevelNumber += 1;
            coins += 1;
            SaveToServer("test", "sf");
        }
    }

    #region 获取数据

    private void LoadData()
    {
        Dictionary<string, object> data = new Dictionary<string, object>();
        data.Add("game_user_id", deviceId);
        DownLoadUserInfo(data);
    }
    
    private void DownLoadUserInfo(Dictionary<string, object> data)
    {
        var url = HttpManager.GetInstance().GetUrl(HttpManager.GET_CONFIG_INTERFACE, data);
        HttpManager.GetInstance().Get(url,
            (string resultUser, long codeUser, string errorUser) =>
            {
                if (!string.IsNullOrEmpty(errorUser))
                {
                    Debug.Log($"DownLoadUserInfo Fail ,Err: {errorUser}");
                }
                else
                {
                    Debug.Log("DownLoadUserInfo Success,data:" + JSON.Parse(resultUser));

                    string str = JSON.Parse(resultUser)["purchase_config"];
                    if (string.IsNullOrEmpty(str))
                    {
                        userInfo = "";
                    }
                    else
                    {
                        userInfo = JSON.Parse(str);
                        if (userInfo != null && userInfo["currentlevel"] != null)
                        {
                            lastCompletedLevelNumber = userInfo["currentlevel"].AsInt;
                        }
                        if (userInfo != null && userInfo["coins"] != null)
                        {
                            coins = userInfo["coins"].AsInt;
                        }
                    }
                }
            });
    }

    #endregion

    #region 上传数据

    public void SaveToServer(string productId,string from)
    {
        Dictionary<string, object> infoDic = new Dictionary<string, object>();

        string device = "wordconnect-";
#if UNITY_EDITOR
        device += "window";
#elif UNITY_IPHONE || UNITY_IOS
        device += "ios";
#elif UNITY_ANDROID
        device += "android";
#endif
        infoDic.Add("game", device);
        infoDic.Add("skuid", productId);
        infoDic.Add("coins", coins);
        infoDic.Add("currentlevel", lastCompletedLevelNumber + 1);
        infoDic.Add("appversion", Application.version);
        infoDic.Add("from", from);
        Dictionary<string, object> dic = new Dictionary<string, object>();
        dic.Add("game_user_id", deviceId);
        string dicStr = Utilities.ConvertToJsonString(infoDic);
        dic.Add("purchase_config", dicStr);
        HttpManager.GetInstance().Post(HttpManager.UPDATE_CONFIG_INTERFACE, dic,
            (string result, long code, string error) =>
            {
                Debug.Log($"post result:{result},code:{code},err:{error}");
            });
    }

    #endregion
    
    
}
