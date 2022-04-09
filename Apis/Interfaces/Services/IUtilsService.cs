﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Apis.Interfaces.Services
{
    public interface IUtilsService
    {

        #region misc        
        string AddLinkPrefix(string linkBody, Apis.Models.Datas.Enums.LinkTypes type);
        string Base64Encode(string plainText);
        string Base64Decode(string b64String);
        string GetLinkBody(string link);

        void ExecuteInParallel<TParam>(
            IEnumerable<TParam> source, Action<TParam> worker);

        void ExecuteInParallel<TParam, TResult>(
            IEnumerable<TParam> source, Func<TParam, TResult> worker);

        string ScanQrcode();
        #endregion
    }
}
