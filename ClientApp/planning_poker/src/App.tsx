import React, { useEffect } from 'react';
import logo from './logo.svg';
import './App.css';
import axios, { AxiosRequestConfig, AxiosPromise, AxiosResponse } from 'axios';

axios.defaults.maxRedirects = 0;
axios.defaults.headers.common['X-Requested-With'] = 'XMLHttpRequest'
axios.defaults.withCredentials = true
axios.interceptors.response.use((response) => {

  return response;
}, (error) => {
  if (error.response.status === 401) {
    window.location.href = error.response.headers.location ;
  }
});

function App() {

  useEffect(() => {
    async function fetchMyAPI() {
      const r = await axios.post<AxiosResponse>('/api/sessions')
      console.log(JSON.stringify(r))
    }
    fetchMyAPI();
  }, []);

  return (
    <div className="App">
      <header className="App-header">
        <img src={logo} className="App-logo" alt="logo" />
        <p>
          Edit <code>src/App.tsx</code> and save to reload.
        </p>
        <a
          className="App-link"
          href="https://reactjs.org"
          target="_blank"
          rel="noopener noreferrer"
        >
          Learn React
        </a>
      </header>
    </div>
  );
}

export default App;
