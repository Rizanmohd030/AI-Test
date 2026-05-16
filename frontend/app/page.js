"use client";

import { useState } from "react";
import styles from "./page.module.css";

export default function Home() {
  const [prompt, setPrompt] = useState("");
  const [extractedData, setExtractedData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [createdQuotation, setCreatedQuotation] = useState(null);
  const [clientResponse, setClientResponse] = useState(null);

  const handleUnifiedPrompt = async () => {
    if (!prompt.trim()) return;
    setLoading(true);
    setError(null);
    setExtractedData(null);
    setCreatedQuotation(null);
    setClientResponse(null);

    try {
      // Step 1: Hit the Client Prompt endpoint which now acts as our master router
      const res = await fetch("http://localhost:5158/api/clients/prompt", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ prompt }),
      });

      const data = await res.json();
      if (!res.ok) throw new Error(data.error || "Failed to process prompt");

      if (data.intent === "create_quotation") {
        // Step 2: If the AI thinks this is a quotation, hit the extraction endpoint
        const extractRes = await fetch("http://localhost:5158/api/quotation/extract", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ prompt }),
        });

        const extractData = await extractRes.json();
        if (!extractRes.ok) throw new Error(extractData.error || "Failed to extract quotation data");
        
        setExtractedData({ ...extractData, originalPrompt: prompt });
      } else {
        // Step 3: Otherwise, it's a client command (Create/Update/Search/List/Delete)
        setClientResponse(data);
      }
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
          <label className={styles.promptLabel}>What would you like to do?</label>
          <div style={{ display: "flex", flexDirection: "column", gap: "5px", marginBottom: "15px", opacity: 0.8, fontSize: "0.85rem" }}>
            <span>💡 "Create a quotation for ABC Corp for website redesign worth ₹50,000"</span>
            <span>💡 "Show me all clients" or "Create client Jane Doe (jane@doe.com)"</span>
          </div>
          <textarea
            className={styles.textarea}
            value={prompt}
            onChange={(e) => setPrompt(e.target.value)}
            placeholder="Type your command (quotation or client management)..."
          />
          <button
            className={styles.button}
            onClick={handleUnifiedPrompt}
            disabled={loading || !prompt}
          >
            {loading ? "Processing..." : "✨ Execute with AI"}
          </button>
        </div>
      )}

      {error && <div className={styles.error}><strong>Error:</strong> {error}</div>}

      {/* Client Result UI (Unified) */}
      {clientResponse && (
        <div className={styles.card} style={{ borderLeft: "4px solid #0070f3" }}>
          <h2 className={styles.sectionTitle}>
            <span style={{ textTransform: "uppercase", fontSize: "0.7rem", background: "#0070f3", color: "white", padding: "3px 8px", borderRadius: "4px", marginRight: "10px", verticalAlign: "middle" }}>
              {clientResponse.intent}
            </span>
            {clientResponse.message}
          </h2>

          {clientResponse.client && (
            <div style={{ marginTop: "15px", background: "rgba(0,0,0,0.05)", padding: "15px", borderRadius: "8px" }}>
              <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "10px" }}>
                <div><strong>Name:</strong> {clientResponse.client.name}</div>
                <div><strong>Email:</strong> {clientResponse.client.email || "—"}</div>
                <div><strong>Phone:</strong> {clientResponse.client.phone || "—"}</div>
                <div><strong>Notes:</strong> {clientResponse.client.notes || "—"}</div>
              </div>
            </div>
          )}

          {clientResponse.clients && clientResponse.clients.length > 0 && (
            <div style={{ marginTop: "15px", display: "flex", flexDirection: "column", gap: "8px" }}>
              {clientResponse.clients.map((c) => (
                <div key={c.id} style={{ background: "rgba(0,0,0,0.02)", padding: "10px", borderRadius: "6px", border: "1px solid rgba(0,0,0,0.05)", display: "flex", justifyContent: "space-between" }}>
                  <span><strong>{c.name}</strong> ({c.email || "No Email"})</span>
                  <span style={{ opacity: 0.5 }}>ID: {c.id}</span>
                </div>
              ))}
            </div>
          )}
          
          <button 
            className={styles.button} 
            style={{ marginTop: "20px", width: "fit-content", background: "transparent", border: "1px solid #ccc", color: "#666" }}
            onClick={() => setClientResponse(null)}
          >
            Close Result
          </button>
        </div>
      )}

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
