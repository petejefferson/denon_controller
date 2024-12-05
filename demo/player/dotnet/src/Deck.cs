﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Un4seen.Bass;

namespace PlayerDemo
{
    public class Deck
    {
        private class Duration
        {
            public byte Minutes;
            public byte Seconds;
            public byte Frames;

            public Duration(double time)
            {
                Minutes = (byte)(Convert.ToInt32(Math.Truncate(time)) / 60);
                Seconds = (byte)(Convert.ToInt32(Math.Truncate(time)) % 60);
                Frames = (byte)(Convert.ToInt32((time - Math.Truncate(time)) * 100));
            }
        }

        private void StutterThread()
        {
            while (true)
            {
                _stutter_ev.WaitOne();
                {
                    Bass.BASS_ChannelSetPosition(BassStream, CuePos);
                    Bass.BASS_ChannelPlay(BassStream, false);
                    Thread.Sleep(100);
                }
            }
        }

        private void TimeThread()
        {
            while (true)
            {
                _time_ev.WaitOne();
                UpdateTime();
                Thread.Sleep(10);
            }
        }

        private ManualResetEvent _stutter_ev;
        private Thread _stutter_thread;

        public ManualResetEvent _time_ev = new ManualResetEvent(false);
        private Thread _time_thread;

        private byte _deck_num = 0;

        public int BassStream = 0;
        public byte TimeMode = 1;
        public long BassDuration = 0;
        public float OrigSampleRate = 0;
        public long CuePos = 0;
        public bool IsPlaying = false;
        public bool IsCueing = false;

        public Deck(byte DeckNum)
        {
            _deck_num = DeckNum;

            _stutter_ev = new ManualResetEvent(false);
            _stutter_thread = new Thread(new ThreadStart(StutterThread));
            _stutter_thread.Start();

            _time_ev = new ManualResetEvent(false);
            _time_thread = new Thread(new ThreadStart(TimeThread));
            _time_thread.Start();
        }

        private void UpdateTime()
        {
            if (BassStream != 0)
            {
                long pos = Bass.BASS_ChannelGetPosition(BassStream);
                long p = TimeMode == 1 ? pos : BassDuration - pos;
                double time = Bass.BASS_ChannelBytes2Seconds(BassStream, p);
                Duration d = new Duration(time);
                Native.UpdateTime(_deck_num, d.Minutes, d.Seconds, d.Frames);
            }
        }

        public void ChangePitch(float NewPitchPercent)
        {
            float targetsamplerate = OrigSampleRate + ((OrigSampleRate / 100) * NewPitchPercent);
            Bass.BASS_ChannelSetAttribute(BassStream, BASSAttribute.BASS_ATTRIB_FREQ, targetsamplerate);
        }

        public void ChangeTime(byte Mode)
        {
            TimeMode = Mode;
            Native.UpdateTimeMode(_deck_num, TimeMode);
            UpdateTime();
        }

        public void PlayPause()
        {
            _stutter_ev.Reset();

            if (!IsPlaying)
            {
                Bass.BASS_ChannelPlay(BassStream, false);
                IsPlaying = true;
                _time_ev.Set();
            }
            else
            {
                _time_ev.Reset();
                Bass.BASS_ChannelPause(BassStream);

                CuePos = Bass.BASS_ChannelGetPosition(BassStream);

                double time = Bass.BASS_ChannelBytes2Seconds(BassStream, CuePos);

                Duration d = new Duration(time);

                Native.Cue(_deck_num, d.Minutes, d.Seconds, d.Frames);
                UpdateTime();

                _stutter_ev.Reset();

                IsPlaying = false;
            }
        }

        public void Cue()
        {
            _time_ev.Reset();
            Bass.BASS_ChannelPause(BassStream);
            Bass.BASS_ChannelSetPosition(BassStream, CuePos);
            double time = Bass.BASS_ChannelBytes2Seconds(BassStream, CuePos);

            Duration d = new Duration(time);

            Native.Cue(_deck_num, d.Minutes, d.Seconds, d.Frames);
            UpdateTime();

            _stutter_ev.Reset();

            IsPlaying = false;
        }

        public void Scan(byte Direction, Byte Speed)
        {
            IsPlaying = false;
            _time_ev.Reset();
            Bass.BASS_ChannelStop(BassStream);
            CuePos = Bass.BASS_ChannelGetPosition(BassStream);
            _stutter_ev.Set();

            double time = Bass.BASS_ChannelBytes2Seconds(BassStream, CuePos);

            if (Direction == 1)
                time = time + (1 * Convert.ToDouble(Speed));
            else
                time = time - (1 * Convert.ToDouble(Speed));

            CuePos = Bass.BASS_ChannelSeconds2Bytes(BassStream, time);

            Bass.BASS_ChannelSetPosition(BassStream, CuePos);
            UpdateTime();
        }

        public void Search(byte Direction, Byte Speed)
        {
            if (IsPlaying)
            {
                IsPlaying = false;
                _time_ev.Reset();
                Bass.BASS_ChannelStop(BassStream);
                CuePos = Bass.BASS_ChannelGetPosition(BassStream);
            }

            _stutter_ev.Reset();
            Bass.BASS_ChannelSetPosition(BassStream, CuePos);
            UpdateTime();

            _stutter_ev.Set();

            double time = Bass.BASS_ChannelBytes2Seconds(BassStream, CuePos);

            if (Direction == 1)
                time = time + (0.01f * Convert.ToDouble(Speed));
            else
                time = time - (0.01f * Convert.ToDouble(Speed));

            CuePos = Bass.BASS_ChannelSeconds2Bytes(BassStream, time);

            Bass.BASS_ChannelSetPosition(BassStream, CuePos);
            UpdateTime();
        }

        public void LoadTrack(string Filename)
        {
            BassStream = Bass.BASS_StreamCreateFile(Filename, 0, 0, BASSFlag.BASS_DEFAULT);
            BassDuration = Bass.BASS_ChannelGetLength(BassStream, BASSMode.BASS_POS_BYTES);
            double time = Bass.BASS_ChannelBytes2Seconds(BassStream, BassDuration);
            Bass.BASS_ChannelGetAttribute(BassStream, BASSAttribute.BASS_ATTRIB_FREQ, ref OrigSampleRate);
            Duration d = new Duration(time);

            Native.Load(_deck_num, d.Minutes, d.Seconds, d.Frames);

            CuePos = 0;

            IsPlaying = false;
        }
    }
}
