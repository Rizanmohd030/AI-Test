"use client";

import { useState } from "react";

export default function Home() {
  const [prompt, setPrompt] = useState("");
  const [extractedData, setExtractedData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [createdQuotation, setCreatedQuotation] = useState(null);

  const handleExtract = async () => {
    if (!prompt.trim()) return;
    setLoading(true);
    setError(null);
    setExtractedData(null);
    setCreatedQuotation(null);

    try {
      const res = await fetch("http://localhost:5158/api/quotation/extract", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ prompt }),
      });

      const data = await res.json();
      if (!res.ok) throw new Error(data.error || "Failed to extract data");

      // Adding OriginalPrompt since we need it for the next step
      setExtractedData({ ...data, originalPrompt: prompt });
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async () => {
    setLoading(true);
    setError(null);

    try {
      const res = await fetch("http://localhost:5158/api/quotation/create", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(extractedData),
      });

      const data = await res.json();
      if (!res.ok) throw new Error(data.error || "Failed to create quotation");

      setCreatedQuotation(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ padding: "40px", fontFamily: "sans-serif", maxWidth: "800px", margin: "0 auto" }}>
      <h1>🤖 AI-Assisted ERP Quotation</h1>
      
      <div style={{ marginBottom: "20px" }}>
        <p>Type a natural language prompt. For example:</p>
        <em style={{ color: "gray" }}>"Create a quotation for ABC Corp for website redesign worth ₹50,000 with 18% GST and delivery in 30 days."</em>
      </div>

      <textarea
        value={prompt}
        onChange={(e) => setPrompt(e.target.value)}
        rows={4}
        style={{ width: "100%", padding: "10px", fontSize: "16px", marginBottom: "10px" }}
        placeholder="Enter your prompt here..."
      />

      <button
        onClick={handleExtract}
        disabled={loading || !prompt}
        style={{ padding: "10px 20px", fontSize: "16px", cursor: "pointer", backgroundColor: "#0070f3", color: "white", border: "none", borderRadius: "5px" }}
      >
        {loading ? "Processing..." : "Extract Data with AI"}
      </button>

      {error && <div style={{ color: "red", marginTop: "20px" }}><strong>Error:</strong> {error}</div>}

      {/* Human Verification Step */}
      {extractedData && !createdQuotation && (
        <div style={{ marginTop: "40px", padding: "20px", border: "1px solid #ddd", borderRadius: "8px", backgroundColor: "#f9f9f9" }}>
          <h2>👀 Human Verification Required</h2>
          <p>Please review the data extracted by the AI before confirming.</p>
          
          <pre style={{ backgroundColor: "#eee", padding: "15px", borderRadius: "5px", overflowX: "auto" }}>
            {JSON.stringify(extractedData, null, 2)}
          </pre>

          <button
            onClick={handleCreate}
            disabled={loading}
            style={{ marginTop: "20px", padding: "10px 20px", fontSize: "16px", cursor: "pointer", backgroundColor: "#28a745", color: "white", border: "none", borderRadius: "5px" }}
          >
            {loading ? "Creating..." : "Looks Good — Create Quotation"}
          </button>
        </div>
      )}

      {/* Success Step */}
      {createdQuotation && (
        <div style={{ marginTop: "40px", padding: "20px", border: "1px solid #28a745", borderRadius: "8px", backgroundColor: "#d4edda" }}>
          <h2 style={{ color: "#155724" }}>✅ Quotation Created Successfully!</h2>
          <p><strong>Quotation Number:</strong> {createdQuotation.quotationNumber}</p>
          <p><strong>Total Amount:</strong> ₹{createdQuotation.totalAmount}</p>
          
          <a
            href={`http://localhost:5158/api/quotation/${createdQuotation.id}/pdf`}
            target="_blank"
            rel="noreferrer"
            style={{ display: "inline-block", marginTop: "15px", padding: "10px 20px", backgroundColor: "#0070f3", color: "white", textDecoration: "none", borderRadius: "5px" }}
          >
            📄 Download PDF
          </a>
        </div>
      )}
    </div>
  );
}
