using ManagedBass;
using System;
using System.Threading;


namespace PlayerDemo
{
    public class Deck : IDisposable
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
            while (_alive)
            {
                if (!_stutter_ev.WaitOne(100))
                {
                    Bass.ChannelSetPosition(BassStream, CuePos);
                    Bass.ChannelPlay(BassStream, false);
                    Thread.Sleep(100);
                }
            }
        }

        private void TimeThread()
        {
            while (_alive)
            {
                if (!_time_ev.WaitOne(10))
                {
                    UpdateTime();
                }
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

        private bool _alive = false;

        public Deck(byte DeckNum)
        {
            _deck_num = DeckNum;

            _stutter_ev = new ManualResetEvent(false);
            _stutter_thread = new Thread(new ThreadStart(StutterThread));
            _stutter_thread.Start();

            _alive = true;

            _time_ev = new ManualResetEvent(false);
            _time_thread = new Thread(new ThreadStart(TimeThread));
            _time_thread.Start();
        }

        private void UpdateTime()
        {
            if (BassStream != 0)
            {
                long pos = Bass.ChannelGetPosition(BassStream);
                long p = TimeMode == 1 ? pos : BassDuration - pos;
                double time = Bass.ChannelBytes2Seconds(BassStream, p);
                Duration d = new Duration(time);
                Native.UpdateTime(_deck_num, d.Minutes, d.Seconds, d.Frames);
            }
        }

        public void ChangePitch(float NewPitchPercent)
        {
            float targetsamplerate = OrigSampleRate + ((OrigSampleRate / 100) * NewPitchPercent);
            Bass.ChannelSetAttribute(BassStream, ChannelAttribute.Frequency, targetsamplerate);
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
                Bass.ChannelPlay(BassStream, false);
                IsPlaying = true;
                _time_ev.Set();
            }
            else
            {
                _time_ev.Reset();
                Bass.ChannelPause(BassStream);

                CuePos = Bass.ChannelGetPosition(BassStream);

                double time = Bass.ChannelBytes2Seconds(BassStream, CuePos);

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
            Bass.ChannelPause(BassStream);
            Bass.ChannelSetPosition(BassStream, CuePos);
            double time = Bass.ChannelBytes2Seconds(BassStream, CuePos);

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
            Bass.ChannelStop(BassStream);
            CuePos = Bass.ChannelGetPosition(BassStream);
            _stutter_ev.Set();

            double time = Bass.ChannelBytes2Seconds(BassStream, CuePos);

            if (Direction == 1)
                time = time + (1 * Convert.ToDouble(Speed));
            else
                time = time - (1 * Convert.ToDouble(Speed));

            CuePos = Bass.ChannelSeconds2Bytes(BassStream, time);

            Bass.ChannelSetPosition(BassStream, CuePos);
            UpdateTime();
        }

        public void Search(byte Direction, Byte Speed)
        {
            if (IsPlaying)
            {
                IsPlaying = false;
                _time_ev.Reset();
                Bass.ChannelStop(BassStream);
                CuePos = Bass.ChannelGetPosition(BassStream);
            }

            _stutter_ev.Reset();
            Bass.ChannelSetPosition(BassStream, CuePos);
            UpdateTime();

            _stutter_ev.Set();

            double time = Bass.ChannelBytes2Seconds(BassStream, CuePos);

            if (Direction == 1)
                time = time + (0.01f * Convert.ToDouble(Speed));
            else
                time = time - (0.01f * Convert.ToDouble(Speed));

            CuePos = Bass.ChannelSeconds2Bytes(BassStream, time);

            Bass.ChannelSetPosition(BassStream, CuePos);
            UpdateTime();
        }

        public void LoadTrack(string Filename)
        {
            BassStream = Bass.CreateStream(Filename, 0, 0, BassFlags.Default);
            BassDuration = Bass.ChannelGetLength(BassStream, PositionFlags.Bytes);
            double time = Bass.ChannelBytes2Seconds(BassStream, BassDuration);
            Bass.ChannelGetAttribute(BassStream, ChannelAttribute.Frequency, out OrigSampleRate);
            Duration d = new Duration(time);

            Native.Load(_deck_num, d.Minutes, d.Seconds, d.Frames);

            CuePos = 0;

            IsPlaying = false;
        }

        public void Dispose()
        {
            _alive = false;
        }
    }
}
