﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Breeze.ChatSummary
{
    public interface IMessagePoster
    {
        Task PostMessageAsync(string message, string roomID);
    }
}
