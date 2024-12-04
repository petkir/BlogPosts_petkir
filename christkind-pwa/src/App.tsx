import React from 'react';
import logo from './logo.svg';
import './App.css';
import Upload from './Upload';
import { Route, BrowserRouter, Routes, Link } from 'react-router-dom';

const Success = () => (
  <div>
    <h1>Upload Successful!</h1>
    <Link to="/">
      <button>Start Over</button>
    </Link>
  </div>
);
function App() {
  return (
    <BrowserRouter>
      <div className="App">
        <Routes>
          <Route path="/" element={<Upload />} />
          <Route path="/success" element={<Success />} />
        </Routes>
      </div>
    </BrowserRouter>
  );
}

export default App;
