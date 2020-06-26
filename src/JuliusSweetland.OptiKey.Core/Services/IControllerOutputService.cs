// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Models;

namespace JuliusSweetland.OptiKey.Services
{
    public interface IControllerOutputService : INotifyPropertyChanged
    {
        Task ProcessKeyPress(string key, KeyPressKeyValue.KeyPressType type);
    }
}
