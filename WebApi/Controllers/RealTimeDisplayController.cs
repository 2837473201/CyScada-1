using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CyScada.BLL;
using CyScada.Model;
using Newtonsoft.Json;

namespace CyScada.Web.WebApi.Controllers
{
    public class RealTimeDisplayController : ApiController
    {
        private BllRealTimeDisplay _bllRealTimeDisplay = new BllRealTimeDisplay();
        
        public BllRealTimeDisplay BLLRealTimeDisplay
        {
            get { return _bllRealTimeDisplay; }
            set { _bllRealTimeDisplay = value; }
        }




        public BaseInfoModel Get(string sideMenuId, string userId)
        {
            return _bllRealTimeDisplay.GetMachineTags(sideMenuId, userId);
        }


        public Hashtable Get()
        {
            return OpcClient.OpcClient.ItemTable;
        }


        public Hashtable Get(string tagList)
        {
            var result = new Hashtable();
            var tagArray = JsonConvert.DeserializeObject<string[]>(tagList);
            foreach (var tagKey in tagArray)
            {
                result.Add(tagKey, OpcClient.OpcClient.ItemTable[tagKey]);
            }
            return result;
        }

        public void Post([FromBody] string[] values)
        {
            OpcClient.OpcClient.SetValue(values[0], values[1]);
        }

    }
}
