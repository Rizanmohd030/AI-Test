"use client";

import { useState } from "react";
import styles from "./page.module.css";

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

  const updateField = (field, value) => {
    setExtractedData(prev => ({ ...prev, [field]: value }));
  };

  const updateLineItem = (index, field, value) => {
    setExtractedData(prev => {
      const newLineItems = [...prev.lineItems];
      // Ensure unitPrice is treated as a number
      if (field === 'unitPrice' || field === 'quantity') {
        newLineItems[index][field] = Number(value);
      } else {
        newLineItems[index][field] = value;
      }
      return { ...prev, lineItems: newLineItems };
    });
  };

  const removeLineItem = (index) => {
    setExtractedData(prev => {
      const newLineItems = prev.lineItems.filter((_, i) => i !== index);
      return { ...prev, lineItems: newLineItems };
    });
  };

  const addLineItem = () => {
    setExtractedData(prev => ({
      ...prev,
      lineItems: [...(prev.lineItems || []), { description: "", quantity: 1, unitPrice: 0 }]
    }));
  };

  return (
    <div className={styles.container}>
      <header className={styles.header}>
        <h1 className={styles.title}>Quotation AI</h1>
        <p className={styles.subtitle}>Generate professional quotations instantly using natural language.</p>
      </header>
      
      {!createdQuotation && (
        <div className={styles.card}>
          <label className={styles.promptLabel}>Describe your quotation requirements</label>
          <span className={styles.promptExample}>"Create a quotation for ABC Corp for website redesign worth ₹50,000 with 18% GST and delivery in 30 days."</span>
          <textarea
            className={styles.textarea}
            value={prompt}
            onChange={(e) => setPrompt(e.target.value)}
            placeholder="Type your prompt here..."
          />
          <button
            className={styles.button}
            onClick={handleExtract}
            disabled={loading || !prompt}
          >
            {loading ? "Processing..." : "✨ Extract Data with AI"}
          </button>
        </div>
      )}

      {error && <div className={styles.error}><strong>Error:</strong> {error}</div>}

      {/* Human Verification Step - Form UI */}
      {extractedData && !createdQuotation && (
        <div className={styles.card}>
          <h2 className={styles.sectionTitle}>👀 Human Verification Required</h2>
          <p style={{ marginBottom: "20px", color: "var(--foreground)", opacity: 0.8 }}>
            Please review and edit the extracted data before generating the document.
          </p>
          
          <div className={styles.formGrid}>
            <div className={styles.formGroup}>
              <label className={styles.label}>Client Name</label>
              <input 
                className={styles.input} 
                value={extractedData.clientName || ""} 
                onChange={(e) => updateField("clientName", e.target.value)} 
              />
            </div>
            <div className={styles.formGroup}>
              <label className={styles.label}>Client Email</label>
              <input 
                className={styles.input} 
                type="email"
                value={extractedData.clientEmail || ""} 
                onChange={(e) => updateField("clientEmail", e.target.value)} 
              />
            </div>
            <div className={styles.formGroup}>
              <label className={styles.label}>Client Phone</label>
              <input 
                className={styles.input} 
                value={extractedData.clientPhone || ""} 
                onChange={(e) => updateField("clientPhone", e.target.value)} 
              />
            </div>
            <div className={styles.formGroup}>
              <label className={styles.label}>Delivery Days</label>
              <input 
                className={styles.input} 
                type="number"
                value={extractedData.deliveryDays || 0} 
                onChange={(e) => updateField("deliveryDays", Number(e.target.value))} 
              />
            </div>
            <div className={styles.formGroup}>
              <label className={styles.label}>GST Percentage (%)</label>
              <input 
                className={styles.input} 
                type="number"
                value={extractedData.gstPercentage || 0} 
                onChange={(e) => updateField("gstPercentage", Number(e.target.value))} 
              />
            </div>
          </div>

          <div className={styles.formGroup} style={{ marginBottom: "30px" }}>
            <label className={styles.label}>Notes</label>
            <textarea 
              className={styles.input} 
              rows={2}
              value={extractedData.notes || ""} 
              onChange={(e) => updateField("notes", e.target.value)} 
            />
          </div>

          <h3 className={styles.sectionTitle}>Line Items</h3>
          <div className={styles.lineItemsContainer}>
            {extractedData.lineItems?.map((item, index) => (
              <div key={index} className={styles.lineItemRow}>
                <div className={styles.formGroup}>
                  <label className={styles.label}>Description</label>
                  <input 
                    className={styles.input} 
                    value={item.description || ""} 
                    onChange={(e) => updateLineItem(index, "description", e.target.value)} 
                  />
                </div>
                <div className={styles.formGroup}>
                  <label className={styles.label}>Quantity</label>
                  <input 
                    className={styles.input} 
                    type="number"
                    value={item.quantity || 0} 
                    onChange={(e) => updateLineItem(index, "quantity", e.target.value)} 
                  />
                </div>
                <div className={styles.formGroup}>
                  <label className={styles.label}>Unit Price (₹)</label>
                  <input 
                    className={styles.input} 
                    type="number"
                    value={item.unitPrice || 0} 
                    onChange={(e) => updateLineItem(index, "unitPrice", e.target.value)} 
                  />
                </div>
                <button 
                  className={styles.iconButton}
                  onClick={() => removeLineItem(index)}
                  title="Remove Item"
                >
                  ✕
                </button>
              </div>
            ))}
            <button 
              className={styles.button} 
              style={{ background: "transparent", color: "#0070f3", border: "1px dashed #0070f3", marginTop: "10px", width: "fit-content" }}
              onClick={addLineItem}
            >
              + Add Line Item
            </button>
          </div>

          <div style={{ display: "flex", justifyContent: "flex-end", marginTop: "30px", paddingTop: "20px", borderTop: "1px solid rgba(255,255,255,0.1)" }}>
            <button
              className={`${styles.button} ${styles.buttonSuccess}`}
              onClick={handleCreate}
              disabled={loading}
              style={{ padding: "15px 30px", fontSize: "1.1rem" }}
            >
              {loading ? "Generating..." : "Approve & Generate Quotation"}
            </button>
          </div>
        </div>
      )}

      {/* Success Step */}
      {createdQuotation && (
        <div className={`${styles.card} ${styles.successCard}`}>
          <div className={styles.successIcon}>🎉</div>
          <h2 style={{ color: "#059669", marginBottom: "20px" }}>Quotation Generated!</h2>
          
          <div className={styles.statGrid}>
            <div className={styles.statItem}>
              <span className={styles.statLabel}>Quotation No.</span>
              <span className={styles.statValue}>{createdQuotation.quotationNumber}</span>
            </div>
            <div className={styles.statItem}>
              <span className={styles.statLabel}>Total Amount</span>
              <span className={styles.statValue}>₹{createdQuotation.totalAmount.toLocaleString()}</span>
            </div>
          </div>
          
          <a
            href={`http://localhost:5158/api/quotation/${createdQuotation.id}/pdf`}
            target="_blank"
            rel="noreferrer"
            className={`${styles.button} ${styles.buttonSuccess}`}
            style={{ textDecoration: "none", marginTop: "10px", display: "inline-flex" }}
          >
            📄 Download PDF
          </a>
        </div>
      )}
    </div>
  );
}
