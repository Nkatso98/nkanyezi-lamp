using System;
using System.Collections.Generic;
using NkanyeziLamp.Api.Models;

namespace NkanyeziLamp.Api.Services
{
    public class WorkflowStateStore
    {
        private readonly Dictionary<string, WorkflowSessionState> _sessions = new();
        private readonly object _lock = new();

        public WorkflowSessionState CreateOrGet(string sessionId, string subject)
        {
            lock (_lock)
            {
                if (!string.IsNullOrWhiteSpace(sessionId) && _sessions.TryGetValue(sessionId, out var existing))
                {
                    if (!string.IsNullOrWhiteSpace(subject))
                    {
                        existing.Subject = subject;
                    }
                    return existing;
                }

                var id = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId;
                var session = new WorkflowSessionState
                {
                    SessionId = id,
                    Subject = subject,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _sessions[id] = session;
                return session;
            }
        }

        public WorkflowSessionState Get(string sessionId)
        {
            lock (_lock)
            {
                _sessions.TryGetValue(sessionId, out var session);
                return session;
            }
        }

        public void Save(WorkflowSessionState session)
        {
            if (session == null || string.IsNullOrWhiteSpace(session.SessionId))
            {
                return;
            }

            lock (_lock)
            {
                _sessions[session.SessionId] = session;
            }
        }
    }
}
