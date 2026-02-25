import React, { useState } from 'react';
import './App.css';

const sections = [
  'Upload Exam',
  'Upload Memo',
  'Review Matched Questions',
  'Edit Teaching Script',
  'Generate Video',
  'Export',
  'History of Generated Videos'
];

function AdminDashboard() {
  const [activeSection, setActiveSection] = useState(sections[0]);

  return (
    <div className="dashboard-container">
      <aside className="sidebar">
        <h2>Nkanyezi Lamp Admin</h2>
        <ul>
          {sections.map(section => (
            <li
              key={section}
              className={activeSection === section ? 'active' : ''}
              onClick={() => setActiveSection(section)}
            >
              {section}
            </li>
          ))}
        </ul>
      </aside>
      <main className="main-content">
        <h1>{activeSection}</h1>
        {/* Section content will be implemented for each feature */}
        <div className="section-content">
          {activeSection === 'Upload Exam' && <div>Upload Question Paper PDF</div>}
          {activeSection === 'Upload Memo' && <div>Upload Memorandum PDF</div>}
          {activeSection === 'Review Matched Questions' && <div>Review and correct question-memo matches</div>}
          {activeSection === 'Edit Teaching Script' && <div>Edit generated teaching scripts before rendering</div>}
          {activeSection === 'Generate Video' && <div>Preview and generate teaching video</div>}
          {activeSection === 'Export' && <div>Download final MP4 video</div>}
          {activeSection === 'History of Generated Videos' && <div>View and manage video history</div>}
        </div>
      </main>
    </div>
  );
}

export default AdminDashboard;
