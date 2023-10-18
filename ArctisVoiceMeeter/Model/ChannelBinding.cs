﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using ArctisVoiceMeeter.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArctisVoiceMeeter.Model;

public partial class ChannelBinding : ObservableObject, IDisposable
{
    public event EventHandler<ChannelBindingOptions> OptionsChanged;

    private readonly VoiceMeeterClient _voiceMeeter;

    [ObservableProperty]
    private ChannelBindingOptions _options;

    private HeadsetChannelBinding[] _boundHeadsets;

    [ObservableProperty] 
    private float _voiceMeeterVolume;

    [ObservableProperty]
    private string _bindingName;



    public ChannelBinding(HeadsetPoller poller, VoiceMeeterClient voiceMeeter, ChannelBindingOptions options)
    {
        HeadsetPoller = poller;
        _voiceMeeter = voiceMeeter;
        Options = options;
        BindingName = Options.BindingName;
        BoundHeadsets = Options.BoundHeadsets.ToArray();

        HeadsetPoller.ArctisStatusChanged += OnHeadsetStatusChanged;
        Options.PropertyChanged += OnOptionsPropertyChanged;
    }

    private void OnOptionsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OptionsChanged?.Invoke(this, (ChannelBindingOptions)sender);
    }

    public HeadsetPoller HeadsetPoller { get; }

    public HeadsetChannelBinding[] BoundHeadsets
    {
        get => _boundHeadsets;
        set
        {
            SetProperty(ref _boundHeadsets, value);
            Options.BoundHeadsets = value.ToList();
        }
    }

    private void OnHeadsetStatusChanged(object? sender, ArctisStatus[] status)
    {
        foreach (var boundHeadset in BoundHeadsets.Where(x=>x.IsEnabled))
        {
            var boundHeadsetStatus = status[boundHeadset.Index];
            UpdateVoiceMeeterGain(boundHeadsetStatus, boundHeadset.BoundChannel);
        }
    }

    private void UpdateVoiceMeeterGain(ArctisStatus arctisStatus, ArctisChannel channel)
    {
        VoiceMeeterVolume = GetScaledChannelVolume(arctisStatus, channel);
        _voiceMeeter.TrySetGain(Options.BoundStrip, VoiceMeeterVolume);
    }

    private float GetScaledChannelVolume(ArctisStatus status, ArctisChannel channel)
    {
        var volume = status.GetArctisVolume(channel);
        return MathHelper.Scale(volume, 0, 100, Options.VoiceMeeterMinVolume, Options.VoiceMeeterMaxVolume);
    }

    public void Dispose()
    {
        HeadsetPoller.ArctisStatusChanged -= OnHeadsetStatusChanged;
        Options.PropertyChanged -= OnOptionsPropertyChanged;
    }
}