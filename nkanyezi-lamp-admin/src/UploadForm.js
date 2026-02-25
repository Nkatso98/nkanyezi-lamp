import React, { useState } from 'react';

function UploadForm({ onUpload }) {
  const [questionPaper, setQuestionPaper] = useState(null);
  const [memorandum, setMemorandum] = useState(null);
  const [subject, setSubject] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!questionPaper || !memorandum || !subject) {
      alert('Please upload both PDFs and enter a subject.');
      return;
    }
    const formData = new FormData();
    formData.append('questionPaper', questionPaper);
    formData.append('memorandum', memorandum);
    formData.append('subject', subject);
    onUpload(formData);
  };

  return (
    <form className="upload-form" onSubmit={handleSubmit}>
      <h2>Step 1: Upload Exam & Memo</h2>
      <label>Subject:<br />
        <input type="text" value={subject} onChange={e => setSubject(e.target.value)} required />
      </label>
      <label>Question Paper (PDF):<br />
        <input type="file" accept="application/pdf" onChange={e => setQuestionPaper(e.target.files[0])} required />
      </label>
      <label>Memorandum (PDF):<br />
        <input type="file" accept="application/pdf" onChange={e => setMemorandum(e.target.files[0])} required />
      </label>
      <button type="submit">Upload & Generate Video</button>
    </form>
  );
}

export default UploadForm;
