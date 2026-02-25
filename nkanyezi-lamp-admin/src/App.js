import React, { useEffect, useState } from 'react';
import './App.css';

const API_BASE = process.env.REACT_APP_API_BASE_URL || 'http://localhost:5000/api/workflow';

function App() {
  const [status, setStatus] = useState('');
  const [subject, setSubject] = useState('');
  const [sessionId, setSessionId] = useState('');
  const [examFile, setExamFile] = useState(null);
  const [memoFile, setMemoFile] = useState(null);
  const [voiceFile, setVoiceFile] = useState(null);
  const [matches, setMatches] = useState([]);
  const [scripts, setScripts] = useState([]);
  const [project, setProject] = useState(null);
  const [videoPath, setVideoPath] = useState('');
  const [youtubeMeta, setYoutubeMeta] = useState(null);
  const [history, setHistory] = useState([]);
  const [logoFile, setLogoFile] = useState(null);
  const [logoEnabled, setLogoEnabled] = useState(false);
  const [logoPosition, setLogoPosition] = useState('top-right');
  const [logoSize, setLogoSize] = useState(12);
  const [ackEnabled, setAckEnabled] = useState(false);
  const [ackText, setAckText] = useState('');
  const [ackPlacement, setAckPlacement] = useState('end');
  const [introText, setIntroText] = useState('');
  const [outroText, setOutroText] = useState('');

  useEffect(() => {
    loadHistory();
  }, []);

  const loadHistory = async () => {
    try {
      const res = await fetch(`${API_BASE}/history`);
      const data = await res.json();
      setHistory(data);
    } catch (err) {
      setStatus(`History load failed: ${err.message}`);
    }
  };

  const uploadExam = async () => {
    if (!examFile || !subject) {
      setStatus('Please choose a question paper and subject.');
      return;
    }
    setStatus('Uploading question paper...');
    const formData = new FormData();
    formData.append('questionPaper', examFile);
    formData.append('subject', subject);
    if (sessionId) {
      formData.append('sessionId', sessionId);
    }
    const res = await fetch(`${API_BASE}/upload/exam`, { method: 'POST', body: formData });
    const data = await res.json();
    setSessionId(data.sessionId);
    setStatus('Question paper uploaded.');
  };

  const uploadMemo = async () => {
    if (!memoFile || !subject || !sessionId) {
      setStatus('Please upload exam first, then memo.');
      return;
    }
    setStatus('Uploading memorandum...');
    const formData = new FormData();
    formData.append('memorandum', memoFile);
    formData.append('subject', subject);
    formData.append('sessionId', sessionId);
    const res = await fetch(`${API_BASE}/upload/memo`, { method: 'POST', body: formData });
    await res.json();
    setStatus('Memorandum uploaded.');
  };

  const uploadVoiceOver = async () => {
    if (!voiceFile || !sessionId) {
      setStatus('Please start a session before uploading voice-over.');
      return;
    }
    setStatus('Uploading voice-over...');
    const formData = new FormData();
    formData.append('voiceOver', voiceFile);
    formData.append('sessionId', sessionId);
    await fetch(`${API_BASE}/upload/voice`, { method: 'POST', body: formData });
    setStatus('Voice-over uploaded. It will be used during rendering.');
  };

  const extractAndMatch = async () => {
    if (!sessionId) {
      setStatus('Upload exam and memo first.');
      return;
    }
    setStatus('Extracting and matching questions...');
    const res = await fetch(`${API_BASE}/process/${sessionId}/extract`, { method: 'POST' });
    const data = await res.json();
    setMatches(data.matches || []);
    setStatus('Matching complete. Review any flagged items.');
  };

  const updateMatches = (index, field, value) => {
    const updated = [...matches];
    updated[index] = { ...updated[index], [field]: value };
    setMatches(updated);
  };

  const saveMatches = async () => {
    setStatus('Saving manual corrections...');
    const res = await fetch(`${API_BASE}/process/${sessionId}/matches`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(matches)
    });
    const data = await res.json();
    setMatches(data.matches || []);
    setStatus('Corrections saved.');
  };

  const generateScripts = async () => {
    setStatus('Generating teaching scripts...');
    const res = await fetch(`${API_BASE}/process/${sessionId}/scripts`, { method: 'POST' });
    const data = await res.json();
    setScripts(data.scripts || []);
    setStatus('Scripts generated. You can edit before rendering.');
  };

  const updateScriptField = (index, field, value) => {
    const updated = [...scripts];
    updated[index] = { ...updated[index], [field]: value };
    setScripts(updated);
  };

  const saveScripts = async () => {
    setStatus('Saving script edits...');
    const payload = scripts.map(script => ({
      questionNumber: script.questionNumber,
      draftScript: script.draftScript,
      commonMistakes: script.commonMistakes,
      marksBreakdown: script.marksBreakdown
    }));
    const res = await fetch(`${API_BASE}/process/${sessionId}/scripts`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    const data = await res.json();
    setScripts(data.scripts || []);
    setStatus('Scripts updated.');
  };

  const buildProject = async () => {
    setStatus('Creating editable video project...');
    const res = await fetch(`${API_BASE}/process/${sessionId}/project`, { method: 'POST' });
    const data = await res.json();
    setProject(data.project);
    hydrateEditor(data.project);
    setStatus('Video project ready. You can edit and render.');
  };

  const hydrateEditor = (proj) => {
    if (!proj) return;
    setIntroText(proj.introText || '');
    setOutroText(proj.outroText || '');
    setLogoEnabled(proj.logo?.enabled || false);
    setLogoPosition(proj.logo?.position || 'top-right');
    setLogoSize(proj.logo?.sizePercent || 12);
    setAckEnabled(proj.acknowledgment?.enabled || false);
    setAckText(proj.acknowledgment?.text || '');
    setAckPlacement(proj.acknowledgment?.placement || 'end');
  };

  const saveProjectEdits = async () => {
    if (!sessionId) return;
    setStatus('Saving project edits...');
    const payload = {
      introText,
      outroText,
      logo: {
        enabled: logoEnabled,
        position: logoPosition,
        sizePercent: Number(logoSize)
      },
      acknowledgment: {
        enabled: ackEnabled,
        text: ackText,
        placement: ackPlacement
      }
    };
    const res = await fetch(`${API_BASE}/process/${sessionId}/project`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    const data = await res.json();
    setProject(data.project);
    setStatus('Project edits saved.');
  };

  const uploadLogo = async () => {
    if (!logoFile || !sessionId) {
      setStatus('Please select a logo and start a session.');
      return;
    }
    setStatus('Uploading logo...');
    const formData = new FormData();
    formData.append('logo', logoFile);
    formData.append('sessionId', sessionId);
    await fetch(`${API_BASE}/upload/logo`, { method: 'POST', body: formData });
    setLogoEnabled(true);
    setStatus('Logo uploaded.');
  };

  const regenerateScript = async (questionNumber) => {
    setStatus(`Regenerating script for Question ${questionNumber}...`);
    const res = await fetch(`${API_BASE}/process/${sessionId}/scripts/regenerate/${questionNumber}`, { method: 'POST' });
    const data = await res.json();
    const updated = scripts.map(script =>
      script.questionNumber === questionNumber ? data.script : script
    );
    setScripts(updated);
    setStatus(`Script regenerated for Question ${questionNumber}.`);
  };

  const renderVideo = async () => {
    setStatus('Rendering video...');
    const res = await fetch(`${API_BASE}/process/${sessionId}/render`, { method: 'POST' });
    const data = await res.json();
    setVideoPath(data.videoPath || '');
    setYoutubeMeta(data.youtubeMeta || null);
    setStatus('Video rendered. Preview and export below.');
    loadHistory();
  };

  const copyToClipboard = async (label, value) => {
    try {
      await navigator.clipboard.writeText(value || '');
      setStatus(`${label} copied to clipboard.`);
    } catch (err) {
      setStatus(`Copy failed: ${err.message}`);
    }
  };

  const resetSession = () => {
    setSessionId('');
    setExamFile(null);
    setMemoFile(null);
    setVoiceFile(null);
    setMatches([]);
    setScripts([]);
    setProject(null);
    setVideoPath('');
    setYoutubeMeta(null);
    setLogoFile(null);
    setLogoEnabled(false);
    setLogoPosition('top-right');
    setLogoSize(12);
    setAckEnabled(false);
    setAckText('');
    setAckPlacement('end');
    setIntroText('');
    setOutroText('');
    setStatus('Started a new session.');
  };

  return (
    <div className="dashboard">
      <header className="hero">
        <h1>Nkanyezi Lamp — CAPS Exam-to-Video Engine</h1>
        <p>Upload exam + memo, review matches, edit teaching scripts, and render your final MP4.</p>
      </header>

      <section className="card">
        <h2>Upload Exam</h2>
        <label>Subject</label>
        <input type="text" value={subject} onChange={e => setSubject(e.target.value)} placeholder="e.g. Physical Sciences" />
        <label>Question Paper (PDF)</label>
        <input type="file" accept="application/pdf" onChange={e => setExamFile(e.target.files[0])} />
        <button onClick={uploadExam}>Upload Question Paper</button>
      </section>

      <section className="card">
        <h2>Upload Memo</h2>
        <label>Memorandum (PDF)</label>
        <input type="file" accept="application/pdf" onChange={e => setMemoFile(e.target.files[0])} />
        <button onClick={uploadMemo} disabled={!sessionId}>Upload Memorandum</button>
      </section>

      <section className="card">
        <h2>Optional Voice-Over</h2>
        <label>Upload Voice Track (MP3/WAV)</label>
        <input type="file" accept="audio/*" onChange={e => setVoiceFile(e.target.files[0])} />
        <button onClick={uploadVoiceOver} disabled={!sessionId}>Upload Voice-Over</button>
        <p className="muted">If provided, this voice track will replace AI narration.</p>
      </section>

      <section className="card">
        <h2>Review Matched Questions</h2>
        <button onClick={extractAndMatch} disabled={!sessionId}>Extract & Match</button>
        {matches.length > 0 && (
          <div className="table">
            {matches.map((match, index) => (
              <div key={match.questionNumber} className={`row ${match.needsReview ? 'flagged' : ''}`}>
                <div>
                  <strong>Q{match.questionNumber}</strong> {match.needsReview && <span className="flag">Needs review</span>}
                  <p>{match.questionText}</p>
                  <label>Memo Answer (editable)</label>
                  <textarea
                    value={match.answerText || ''}
                    onChange={e => updateMatches(index, 'answerText', e.target.value)}
                  />
                  <div className="meta">
                    <span>Match: {match.matchReason}</span>
                    <span>Similarity: {match.similarityScore?.toFixed(2)}</span>
                  </div>
                </div>
              </div>
            ))}
            <button onClick={saveMatches}>Save Manual Corrections</button>
          </div>
        )}
      </section>

      <section className="card">
        <h2>Edit Teaching Script</h2>
        <button onClick={generateScripts} disabled={matches.length === 0}>Generate Scripts</button>
        {scripts.length > 0 && (
          <div className="scripts">
            {scripts.map((script, index) => (
              <div key={script.questionNumber} className="script-card">
                <h3>Question {script.questionNumber}</h3>
                <textarea
                  value={script.draftScript || ''}
                  onChange={e => updateScriptField(index, 'draftScript', e.target.value)}
                />
                <label>Common Mistakes</label>
                <input
                  type="text"
                  value={script.commonMistakes || ''}
                  onChange={e => updateScriptField(index, 'commonMistakes', e.target.value)}
                />
                <label>Marks Breakdown</label>
                <input
                  type="text"
                  value={script.marksBreakdown || ''}
                  onChange={e => updateScriptField(index, 'marksBreakdown', e.target.value)}
                />
                <button onClick={() => regenerateScript(script.questionNumber)}>Regenerate Question</button>
              </div>
            ))}
            <button onClick={saveScripts}>Save Script Updates</button>
          </div>
        )}
      </section>

      <section className="card">
        <h2>Create Editable Video Project</h2>
        <button onClick={buildProject} disabled={scripts.length === 0}>Build Video Project</button>
        {project && <p className="muted">Project ready. Use the editor panel below to customize.</p>}
      </section>

      <section className="card">
        <h2>Video Editor (Simple)</h2>
        <label>Intro Text</label>
        <textarea value={introText} onChange={e => setIntroText(e.target.value)} />
        <label>Outro Text</label>
        <textarea value={outroText} onChange={e => setOutroText(e.target.value)} />
        <div className="editor-grid">
          <div>
            <h3>Logo</h3>
            <label>
              <input type="checkbox" checked={logoEnabled} onChange={e => setLogoEnabled(e.target.checked)} />
              Enable logo overlay
            </label>
            <label>Upload Logo (PNG)</label>
            <input type="file" accept="image/png" onChange={e => setLogoFile(e.target.files[0])} />
            <button onClick={uploadLogo} disabled={!sessionId}>Upload Logo</button>
            <label>Logo Position</label>
            <select value={logoPosition} onChange={e => setLogoPosition(e.target.value)}>
              <option value="top-left">Top left</option>
              <option value="top-right">Top right</option>
              <option value="bottom-left">Bottom left</option>
              <option value="bottom-right">Bottom right</option>
            </select>
            <label>Logo Size (%)</label>
            <input type="number" min="5" max="40" value={logoSize} onChange={e => setLogoSize(e.target.value)} />
          </div>
          <div>
            <h3>Acknowledgments</h3>
            <label>
              <input type="checkbox" checked={ackEnabled} onChange={e => setAckEnabled(e.target.checked)} />
              Enable acknowledgment slide
            </label>
            <label>Acknowledgment Text</label>
            <textarea value={ackText} onChange={e => setAckText(e.target.value)} />
            <label>Placement</label>
            <select value={ackPlacement} onChange={e => setAckPlacement(e.target.value)}>
              <option value="start">Start</option>
              <option value="end">End</option>
            </select>
          </div>
        </div>
        <button onClick={saveProjectEdits} disabled={!project}>Save Editor Changes</button>
      </section>

      <section className="card">
        <h2>Generate Video</h2>
        <button onClick={renderVideo} disabled={!project}>Render Full MP4</button>
      </section>

      <section className="card">
        <h2>Preview & Export</h2>
        {videoPath ? (
          <>
            <video controls src={`${API_BASE}/video/${sessionId}`} className="video-preview" />
            <a className="download" href={`${API_BASE}/video/${sessionId}`} download>Download MP4</a>
          </>
        ) : (
          <p>No video generated yet.</p>
        )}
      </section>

      <section className="card">
        <h2>YouTube Optimization</h2>
        {youtubeMeta ? (
          <div className="meta-block">
            <div className="meta-row">
              <p><strong>Title:</strong> {youtubeMeta.title}</p>
              <button onClick={() => copyToClipboard('Title', youtubeMeta.title)}>Copy</button>
            </div>
            <div className="meta-row">
              <p><strong>Description:</strong> {youtubeMeta.description}</p>
              <button onClick={() => copyToClipboard('Description', youtubeMeta.description)}>Copy</button>
            </div>
            <div className="meta-row">
              <p><strong>Hashtags:</strong> {youtubeMeta.hashtags}</p>
              <button onClick={() => copyToClipboard('Hashtags', youtubeMeta.hashtags)}>Copy</button>
            </div>
            <div className="meta-row">
              <p><strong>Tags:</strong> {youtubeMeta.tags}</p>
              <button onClick={() => copyToClipboard('Tags', youtubeMeta.tags)}>Copy</button>
            </div>
            <div className="meta-row">
              <p><strong>Thumbnail Text:</strong> {youtubeMeta.thumbnailText}</p>
              <button onClick={() => copyToClipboard('Thumbnail text', youtubeMeta.thumbnailText)}>Copy</button>
            </div>
            <a className="upload-link" href="https://www.youtube.com/upload" target="_blank" rel="noreferrer">
              Open YouTube Upload
            </a>
          </div>
        ) : (
          <p>Generate a video to get YouTube metadata.</p>
        )}
      </section>

      <section className="card">
        <h2>History of Generated Videos</h2>
        {history.length === 0 ? (
          <p>No videos generated yet.</p>
        ) : (
          <ul className="history">
            {history.map(item => (
              <li key={item.id}>
                <strong>{item.title}</strong> — {item.subject}
                <div className="muted">{item.videoPath}</div>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="card">
        <h2>Session Controls</h2>
        <div className="session-meta">
          <div><strong>Session:</strong> {sessionId || 'Not started'}</div>
        </div>
        <button className="secondary" onClick={resetSession}>Start New Session</button>
      </section>

      {status && <p className="status"><strong>Status:</strong> {status}</p>}
    </div>
  );
}

export default App;