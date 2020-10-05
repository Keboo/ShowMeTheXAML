﻿using System;

namespace ShowMeTheXAML
{
    public class XamlResolveEventArgs : EventArgs
    {
        public XamlResolveEventArgs(string key) => Key = key;

        public string Xaml { get; set; }
        public string Key { get; }
    }
}