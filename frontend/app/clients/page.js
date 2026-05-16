"use client";

import { useState } from "react";
import styles from "../page.module.css";

export default function Clients() {
  const [prompt, setPrompt] = useState("");
  const [response, setResponse] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const handlePrompt = async () => {
    if (!prompt.trim()) return;
    setLoading(true);
    setError(null);
    setResponse(null);

    try {
      // Calling the C# Backend directly!
      const res = await fetch("http://localhost:5158/api/clients/prompt", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ prompt }),
      });

      const data = await res.json();
      if (!res.ok) throw new Error(data.error || "Failed to process prompt");

      setResponse(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={styles.container}>
      <header className={styles.header}>
        <h1 className={styles.title}>Client AI Manager</h1>
        <p className={styles.subtitle}>Manage your clients instantly using natural language.</p>
      </header>

      <div className={styles.card}>
        <label className={styles.promptLabel}>What would you like to do?</label>
        <div style={{ display: "flex", flexDirection: "column", gap: "8px", marginBottom: "16px" }}>
          <span className={styles.promptExample}>💡 "Create a client named Sarah Smith with email sarah@example.com"</span>
          <span className={styles.promptExample}>💡 "Show me all clients"</span>
          <span className={styles.promptExample}>💡 "What was the last client we created?"</span>
        </div>
        
        <textarea
          className={styles.textarea}
          value={prompt}
          onChange={(e) => setPrompt(e.target.value)}
          placeholder="Type your command here..."
          rows={3}
        />
        <button
          className={styles.button}
          onClick={handlePrompt}
          disabled={loading || !prompt}
        >
          {loading ? "Processing..." : "🚀 Execute Command"}
        </button>
      </div>

      {error && <div className={styles.error}><strong>Error:</strong> {error}</div>}

      {response && (
        <div className={styles.card} style={{ marginTop: "24px" }}>
          <h2 className={styles.sectionTitle}>
            <span style={{ textTransform: "uppercase", fontSize: "0.8rem", background: "var(--foreground)", color: "var(--background)", padding: "4px 8px", borderRadius: "4px", marginRight: "10px", verticalAlign: "middle" }}>
              {response.intent}
            </span>
            {response.message}
          </h2>

          {/* Display Single Client */}
          {response.client && (
            <div style={{ marginTop: "20px", background: "rgba(255,255,255,0.05)", padding: "20px", borderRadius: "8px", border: "1px solid rgba(255,255,255,0.1)" }}>
              <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "16px" }}>
                <div>
                  <div style={{ fontSize: "0.9rem", opacity: 0.7 }}>Name</div>
                  <div style={{ fontSize: "1.2rem", fontWeight: "bold" }}>{response.client.name}</div>
                </div>
                <div>
                  <div style={{ fontSize: "0.9rem", opacity: 0.7 }}>Email</div>
                  <div>{response.client.email || "—"}</div>
                </div>
                <div>
                  <div style={{ fontSize: "0.9rem", opacity: 0.7 }}>Phone</div>
                  <div>{response.client.phone || "—"}</div>
                </div>
                <div>
                  <div style={{ fontSize: "0.9rem", opacity: 0.7 }}>Notes</div>
                  <div>{response.client.notes || "—"}</div>
                </div>
              </div>
            </div>
          )}

          {/* Display List of Clients */}
          {response.clients && response.clients.length > 0 && (
            <div style={{ marginTop: "20px", display: "flex", flexDirection: "column", gap: "12px" }}>
              {response.clients.map((c) => (
                <div key={c.id} style={{ background: "rgba(255,255,255,0.05)", padding: "16px", borderRadius: "8px", border: "1px solid rgba(255,255,255,0.1)", display: "grid", gridTemplateColumns: "1fr 1fr 1fr", alignItems: "center" }}>
                  <div style={{ fontWeight: "bold", fontSize: "1.1rem" }}>{c.name}</div>
                  <div>{c.email}</div>
                  <div style={{ opacity: 0.6, fontSize: "0.9rem" }}>ID: {c.id}</div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
