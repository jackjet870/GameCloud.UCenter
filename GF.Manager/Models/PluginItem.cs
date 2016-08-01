﻿using System;
using System.Collections.Generic;
using System.Net.Http;

namespace GF.Manager.Models
{
    public class PluginItem
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string ControllerName { get; set; }

        public string ActionName { get; set; }

        public HttpMethod Method { get; set; }

        public IReadOnlyList<PluginItem> Items { get; set; }

        //private class CollectionNameAttribute : Attribute
        //{
        //}
    }
}