import React from 'react';
import { render, screen } from '@testing-library/react';
import App from './App';

test('renders Nkanyezi Lamp Admin Dashboard', () => {
  render(<App />);
  const linkElement = screen.getByText(/Nkanyezi Lamp/i);
  expect(linkElement).toBeInTheDocument();
});
