using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;
using Bender.Configuration;
using Bender.Interfaces;
using Bender.Persistence;

namespace Bender.Module
{
    [Export(typeof(IModule))]
    public class Timer : IModule
    {
        // TODO: Allow user to specify exact time

        private static readonly Regex TimerRegex = new Regex(@"^\s*timer\s+(.+)\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex FromNowRegex = new Regex(@"^\s*(\-?[0-9\.]+)\s(ticks?|(swatch )?\.?beats?|minutes?|seconds?|hours?)\sfrom\snow\s+say\s+(.+)\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex DeleteRegex = new Regex(@"^\s*delete\s+all\s*$", RegexOptions.IgnoreCase);

        private IBackend _backend;
        private readonly ConcurrentDictionary<System.Timers.Timer, bool> _activeTimers = new ConcurrentDictionary<System.Timers.Timer, bool>();

        public void OnStart(IConfiguration config, IBackend backend, IKeyValuePersistence persistence)
        {
            _backend = backend;
        }

        public void OnMessage(IMessage message)
        {
            TestTimer(message);
        }

        private void TestTimer(IMessage message)
        {
            if (message.IsRelevant && !message.IsHistorical)
            {
                var match = TimerRegex.Match(message.Body);
                var timerBody = match.Groups[1].Value;
                if (match.Success)
                {
                    double milliseconds; string text;
                    if (TestTimeFromNow(timerBody, out milliseconds, out text))
                    {
                        if (!ValidateTime(message.ReplyTo, milliseconds)) return;
                        AddTimer(message.ReplyTo, milliseconds, text);
                        _backend.SendMessageAsync(message.ReplyTo, "Sure!");
                    }
                    else if (TestDelete(timerBody))
                    {
                        if (_activeTimers.Keys.Any())
                        {
                            foreach (var t in _activeTimers.Keys)
                            {
                                RemoveAndDisposeTimer(t);
                            }
                            _backend.SendMessageAsync(message.ReplyTo, "All timers deleted!");
                        }
                        else
                        {
                            _backend.SendMessageAsync(message.ReplyTo, "No timers to delete.");
                        }
                    }
                }
            }
        }

        private void AddTimer(IAddress replyTo, double milliseconds, string text)
        {
            _activeTimers.AddOrUpdate(CreateTimer(replyTo, milliseconds, text), x => true, (x,y) => true);
        }

        private void RemoveAndDisposeTimer(System.Timers.Timer t)
        {
            bool value;
            _activeTimers.TryRemove(t, out value);
            t.Enabled = false;
            t.Dispose();
        }

        private System.Timers.Timer CreateTimer(IAddress replyTo, double milliseconds, string text)
        {
            var t = new System.Timers.Timer { Interval = milliseconds };
            t.Elapsed += GetElapsedEvent(t, replyTo, text);
            t.Enabled = true;
            return t;
        }

        private bool ValidateTime(IAddress replyTo, double milliseconds)
        {
            if (milliseconds < 0)
            {
                _backend.SendMessageAsync(replyTo, "I can't travel back in time, silly!");
                return false;
            }
            if (milliseconds == 0)
            {
                _backend.SendMessageAsync(replyTo, "Sorry, something weird occurred. I couldn't set up that timer for you.");
                return false;
            }
            else if (milliseconds > 24 * 60 * 60 * 1000)
            {
                _backend.SendMessageAsync(replyTo, "Let's keep the timer to within one day for now.");
                return false;
            }
            return true;
        }

        private static bool TestTimeFromNow(string timerBody, out double milliseconds, out string text)
        {
            milliseconds = 0; text = "";
            
            var match = FromNowRegex.Match(timerBody);
            if (match.Success)
            {
                double num;
                double.TryParse(match.Groups[1].Value, out num);
                
                text = match.Groups[4].Value;

                var units = match.Groups[2].Value;
                if (units.StartsWith("second"))
                {
                    milliseconds = num * 1000;
                }
                else if (units.StartsWith("minute"))
                {
                    milliseconds = num * 60 * 1000;
                }
                else if (units.StartsWith("hour"))
                {
                    milliseconds = num * 60 * 60 * 1000;
                }
                else if (units.StartsWith("tick"))
                {
                    milliseconds = num / TimeSpan.TicksPerMillisecond;
                }
                else if (units.Contains("beat"))
                {
                    milliseconds = num * 86.4 * 1000;
                }

                return true;
            }
            return false;
        }

        private static bool TestDelete(string timerBody)
        {
            return DeleteRegex.Match(timerBody).Success;
        }

        private ElapsedEventHandler GetElapsedEvent(System.Timers.Timer t,  IAddress replyTo, string text)
        {
            return (o, e) =>
            {
                RemoveAndDisposeTimer(t);
                _backend.SendMessageAsync(replyTo, text);
            };
        }
    }
}
