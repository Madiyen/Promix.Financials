import React, { useState } from "react";
import { X, Plus, Minus, SlidersHorizontal, Clock, CheckCircle, AlertCircle } from "lucide-react";

// ─── Exported Types ────────────────────────────────────────────────────────

export interface AuditEntry {
  id: string;
  sku: string;
  date: string;
  time: string;
  user: string;
  adjType: "Add" | "Remove" | "Set To";
  before: number;
  after: number;
  change: number;
  reason: string;
  refNum: string;
  notes: string;
  unit: string;
}

export interface AdjustableItem {
  sku: string;
  name: string;
  category: string;
  inStock: number;
  unit: string;
  reorderLevel: number;
  unitCost: number;
}

// ─── Props ─────────────────────────────────────────────────────────────────

interface Props {
  item: AdjustableItem;
  auditTrail: AuditEntry[];
  onClose: () => void;
  onConfirm: (sku: string, newQty: number, entry: AuditEntry) => void;
}

const REASONS = [
  "Purchase Receipt",
  "Sales Return",
  "Damage / Write-off",
  "Stock Count Correction",
  "Transfer In",
  "Transfer Out",
  "Manual Adjustment",
];

const ADJ_TYPES = ["Add", "Remove", "Set To"] as const;
type AdjType = typeof ADJ_TYPES[number];

// ─── Component ─────────────────────────────────────────────────────────────

export function AdjustStockModal({ item, auditTrail, onClose, onConfirm }: Props) {
  const [adjType, setAdjType] = useState<AdjType>("Add");
  const [qty, setQty] = useState(1);
  const [reason, setReason] = useState(REASONS[0]);
  const [refNum, setRefNum] = useState("");
  const [notes, setNotes] = useState("");

  const newQty =
    adjType === "Add" ? item.inStock + qty :
    adjType === "Remove" ? Math.max(0, item.inStock - qty) :
    qty;
  const change = newQty - item.inStock;
  const changeColor = change > 0 ? "#10B981" : change < 0 ? "#EF4444" : "#94A3B8";

  const adjColors: Record<AdjType, { border: string; bg: string; text: string }> = {
    "Add":     { border: "#10B981", bg: "#ECFDF5", text: "#059669" },
    "Remove":  { border: "#EF4444", bg: "#FEF2F2", text: "#DC2626" },
    "Set To":  { border: "#3B82F6", bg: "#EFF6FF", text: "#2563EB" },
  };

  const handleConfirm = () => {
    const now = new Date();
    const entry: AuditEntry = {
      id: `adj-${Date.now()}`,
      sku: item.sku,
      date: now.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" }),
      time: now.toLocaleTimeString("en-US", { hour: "2-digit", minute: "2-digit" }),
      user: "Ahmed Hassan",
      adjType,
      before: item.inStock,
      after: newQty,
      change,
      reason,
      refNum,
      notes,
      unit: item.unit,
    };
    onConfirm(item.sku, newQty, entry);
  };

  return (
    <div
      onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
      style={{ position: "fixed", inset: 0, backgroundColor: "rgba(15,23,42,0.6)", backdropFilter: "blur(3px)", zIndex: 100, display: "flex", alignItems: "center", justifyContent: "center", fontFamily: "'Inter', sans-serif" }}
    >
      <div style={{ width: 580, maxHeight: "88vh", backgroundColor: "#FFFFFF", borderRadius: 16, boxShadow: "0 32px 80px rgba(0,0,0,0.28)", display: "flex", flexDirection: "column", overflow: "hidden" }}>

        {/* ── Header ── */}
        <div style={{ padding: "20px 24px 16px", borderBottom: "1px solid #F1F5F9", display: "flex", alignItems: "flex-start", justifyContent: "space-between", flexShrink: 0 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
            <div style={{ width: 40, height: 40, borderRadius: 10, backgroundColor: "#EFF6FF", display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
              <SlidersHorizontal size={20} style={{ color: "#3B82F6" }} />
            </div>
            <div>
              <div style={{ color: "#0F172A", fontSize: 15, fontWeight: 800 }}>Adjust Stock</div>
              <div style={{ color: "#94A3B8", fontSize: 11.5, marginTop: 1 }}>Update inventory quantity with full audit trail</div>
            </div>
          </div>
          <button onClick={onClose} style={{ width: 32, height: 32, borderRadius: 8, border: "none", backgroundColor: "#F1F5F9", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center" }}>
            <X size={15} style={{ color: "#64748B" }} />
          </button>
        </div>

        {/* ── Scrollable Body ── */}
        <div style={{ flex: 1, overflowY: "auto", padding: "20px 24px" }}>

          {/* Item Info Banner */}
          <div style={{ backgroundColor: "#F8FAFC", borderRadius: 10, padding: "12px 16px", marginBottom: 20, display: "flex", alignItems: "center", gap: 14, border: "1px solid #E2E8F0" }}>
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ color: "#3B82F6", fontSize: 10.5, fontWeight: 700, fontFamily: "monospace", marginBottom: 3 }}>{item.sku}</div>
              <div style={{ color: "#0F172A", fontSize: 13.5, fontWeight: 700, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{item.name}</div>
              <div style={{ color: "#94A3B8", fontSize: 11, marginTop: 2 }}>{item.category} · ${item.unitCost.toFixed(2)} per {item.unit}</div>
            </div>
            <div style={{ textAlign: "right", flexShrink: 0, paddingLeft: 14, borderLeft: "1px solid #E2E8F0" }}>
              <div style={{ color: "#94A3B8", fontSize: 10, marginBottom: 3 }}>Current Stock</div>
              <div style={{ color: "#0F172A", fontSize: 22, fontWeight: 800, lineHeight: 1 }}>
                {item.inStock} <span style={{ fontSize: 12, color: "#94A3B8", fontWeight: 400 }}>{item.unit}</span>
              </div>
              <div style={{ color: "#94A3B8", fontSize: 10, marginTop: 2 }}>Reorder at {item.reorderLevel}</div>
            </div>
          </div>

          {/* Adjustment Type */}
          <div style={{ marginBottom: 16 }}>
            <label style={{ color: "#374151", fontSize: 12, fontWeight: 600, display: "block", marginBottom: 8 }}>Adjustment Type</label>
            <div style={{ display: "flex", gap: 8 }}>
              {ADJ_TYPES.map(type => {
                const isActive = adjType === type;
                const colors = adjColors[type];
                return (
                  <button key={type}
                    onClick={() => { setAdjType(type); setQty(type === "Set To" ? item.inStock : 1); }}
                    style={{ flex: 1, padding: "9px 0", borderRadius: 8, border: `2px solid ${isActive ? colors.border : "#E2E8F0"}`, backgroundColor: isActive ? colors.bg : "#FFFFFF", color: isActive ? colors.text : "#94A3B8", fontSize: 12.5, fontWeight: 700, cursor: "pointer", fontFamily: "'Inter', sans-serif", transition: "all 0.12s" }}>
                    {type}
                  </button>
                );
              })}
            </div>
          </div>

          {/* Quantity + Preview */}
          <div style={{ marginBottom: 16 }}>
            <label style={{ color: "#374151", fontSize: 12, fontWeight: 600, display: "block", marginBottom: 8 }}>
              {adjType === "Set To" ? "Set Quantity To" : "Quantity"}
            </label>
            <div style={{ display: "flex", gap: 12, alignItems: "stretch" }}>
              {/* Stepper */}
              <div style={{ display: "flex", alignItems: "center", border: "1.5px solid #E2E8F0", borderRadius: 9, overflow: "hidden", backgroundColor: "#FFFFFF", height: 46 }}>
                <button
                  onClick={() => setQty(q => Math.max(adjType === "Set To" ? 0 : 1, q - 1))}
                  style={{ width: 40, height: "100%", border: "none", backgroundColor: "#F8FAFC", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center", borderRight: "1px solid #E2E8F0" }}>
                  <Minus size={14} style={{ color: "#64748B" }} />
                </button>
                <input type="number" value={qty}
                  onChange={e => setQty(Math.max(0, parseInt(e.target.value) || 0))}
                  style={{ width: 64, height: "100%", textAlign: "center", border: "none", outline: "none", fontSize: 17, fontWeight: 800, color: "#0F172A", fontFamily: "'Inter', sans-serif" }} />
                <button
                  onClick={() => setQty(q => q + 1)}
                  style={{ width: 40, height: "100%", border: "none", backgroundColor: "#F8FAFC", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center", borderLeft: "1px solid #E2E8F0" }}>
                  <Plus size={14} style={{ color: "#64748B" }} />
                </button>
              </div>

              {/* Preview Card */}
              <div style={{ flex: 1, backgroundColor: "#F8FAFC", borderRadius: 9, padding: "10px 16px", border: "1.5px solid #E2E8F0", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <div>
                  <div style={{ color: "#94A3B8", fontSize: 10, marginBottom: 3 }}>New Quantity</div>
                  <div style={{ color: "#0F172A", fontSize: 20, fontWeight: 800, lineHeight: 1 }}>
                    {newQty} <span style={{ fontSize: 11, color: "#94A3B8", fontWeight: 400 }}>{item.unit}</span>
                  </div>
                </div>
                <div style={{ textAlign: "right" }}>
                  <div style={{ color: "#94A3B8", fontSize: 10, marginBottom: 3 }}>Δ Change</div>
                  <div style={{ color: changeColor, fontSize: 15, fontWeight: 800 }}>{change > 0 ? `+${change}` : change}</div>
                </div>
              </div>
            </div>

            {/* Warning if removing below reorder */}
            {adjType === "Remove" && newQty <= item.reorderLevel && newQty > 0 && (
              <div style={{ marginTop: 8, display: "flex", alignItems: "center", gap: 6, padding: "6px 10px", borderRadius: 7, backgroundColor: "#FFFBEB", border: "1px solid #FDE68A" }}>
                <AlertCircle size={12} style={{ color: "#F59E0B", flexShrink: 0 }} />
                <span style={{ color: "#92400E", fontSize: 11 }}>New quantity is at or below the reorder level ({item.reorderLevel} {item.unit})</span>
              </div>
            )}
            {adjType !== "Set To" && newQty === 0 && (
              <div style={{ marginTop: 8, display: "flex", alignItems: "center", gap: 6, padding: "6px 10px", borderRadius: 7, backgroundColor: "#FEF2F2", border: "1px solid #FECACA" }}>
                <AlertCircle size={12} style={{ color: "#EF4444", flexShrink: 0 }} />
                <span style={{ color: "#991B1B", fontSize: 11 }}>This will mark the item as out of stock</span>
              </div>
            )}
          </div>

          {/* Reason + Ref */}
          <div style={{ display: "flex", gap: 12, marginBottom: 14 }}>
            <div style={{ flex: 2 }}>
              <label style={{ color: "#374151", fontSize: 12, fontWeight: 600, display: "block", marginBottom: 7 }}>Reason <span style={{ color: "#EF4444" }}>*</span></label>
              <select value={reason} onChange={e => setReason(e.target.value)}
                style={{ width: "100%", padding: "9px 12px", borderRadius: 8, border: "1.5px solid #E2E8F0", fontSize: 12.5, color: "#334155", backgroundColor: "#FFFFFF", outline: "none", fontFamily: "'Inter', sans-serif", cursor: "pointer" }}>
                {REASONS.map(r => <option key={r} value={r}>{r}</option>)}
              </select>
            </div>
            <div style={{ flex: 1 }}>
              <label style={{ color: "#374151", fontSize: 12, fontWeight: 600, display: "block", marginBottom: 7 }}>Reference #</label>
              <input type="text" placeholder="PO-2026-099" value={refNum}
                onChange={e => setRefNum(e.target.value)}
                style={{ width: "100%", padding: "9px 12px", borderRadius: 8, border: "1.5px solid #E2E8F0", fontSize: 12.5, color: "#334155", outline: "none", fontFamily: "'Inter', sans-serif", boxSizing: "border-box" }} />
            </div>
          </div>

          {/* Notes */}
          <div style={{ marginBottom: 20 }}>
            <label style={{ color: "#374151", fontSize: 12, fontWeight: 600, display: "block", marginBottom: 7 }}>Notes</label>
            <textarea placeholder="Optional remarks or context for this adjustment..." value={notes}
              onChange={e => setNotes(e.target.value)} rows={2}
              style={{ width: "100%", padding: "9px 12px", borderRadius: 8, border: "1.5px solid #E2E8F0", fontSize: 12.5, color: "#334155", outline: "none", fontFamily: "'Inter', sans-serif", resize: "none", boxSizing: "border-box" }} />
          </div>

          {/* ── Audit Trail ── */}
          <div style={{ borderTop: "1px solid #F1F5F9", paddingTop: 16 }}>
            <div style={{ display: "flex", alignItems: "center", gap: 6, marginBottom: 10 }}>
              <Clock size={13} style={{ color: "#94A3B8" }} />
              <span style={{ color: "#64748B", fontSize: 11.5, fontWeight: 600 }}>Adjustment History for this Item</span>
              {auditTrail.length > 0 && (
                <span style={{ backgroundColor: "#F1F5F9", color: "#64748B", fontSize: 10, fontWeight: 700, padding: "1px 6px", borderRadius: 8 }}>{auditTrail.length}</span>
              )}
            </div>

            {auditTrail.length === 0 ? (
              <div style={{ textAlign: "center", padding: "20px 0" }}>
                <Clock size={22} style={{ color: "#CBD5E1", margin: "0 auto 7px" }} />
                <div style={{ color: "#94A3B8", fontSize: 12 }}>No adjustments recorded for this item yet</div>
              </div>
            ) : (
              <div style={{ display: "flex", flexDirection: "column", gap: 7 }}>
                {auditTrail.slice(0, 5).map((entry) => (
                  <div key={entry.id} style={{ display: "flex", alignItems: "center", gap: 10, padding: "9px 12px", borderRadius: 9, backgroundColor: "#F8FAFC", border: "1px solid #F1F5F9" }}>
                    <div style={{ width: 30, height: 30, borderRadius: 8, backgroundColor: entry.change > 0 ? "#ECFDF5" : entry.change < 0 ? "#FEF2F2" : "#EFF6FF", display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
                      <CheckCircle size={13} style={{ color: entry.change > 0 ? "#10B981" : entry.change < 0 ? "#EF4444" : "#3B82F6" }} />
                    </div>
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div style={{ color: "#0F172A", fontSize: 12, fontWeight: 600 }}>{entry.reason}</div>
                      <div style={{ color: "#94A3B8", fontSize: 10.5, marginTop: 1 }}>{entry.date} · {entry.time} · <span style={{ fontWeight: 600 }}>{entry.user}</span>{entry.refNum ? ` · ${entry.refNum}` : ""}</div>
                    </div>
                    <div style={{ textAlign: "right", flexShrink: 0 }}>
                      <div style={{ color: entry.change > 0 ? "#10B981" : entry.change < 0 ? "#EF4444" : "#3B82F6", fontSize: 13, fontWeight: 800 }}>
                        {entry.change > 0 ? `+${entry.change}` : entry.change}
                      </div>
                      <div style={{ color: "#94A3B8", fontSize: 10 }}>{entry.before} → {entry.after} {entry.unit}</div>
                    </div>
                  </div>
                ))}
                {auditTrail.length > 5 && (
                  <div style={{ textAlign: "center", color: "#94A3B8", fontSize: 11, paddingTop: 4 }}>+ {auditTrail.length - 5} more entries</div>
                )}
              </div>
            )}
          </div>
        </div>

        {/* ── Footer ── */}
        <div style={{ padding: "14px 24px", borderTop: "1px solid #F1F5F9", display: "flex", gap: 10, justifyContent: "flex-end", flexShrink: 0 }}>
          <button onClick={onClose}
            style={{ padding: "9px 20px", borderRadius: 9, border: "1.5px solid #E2E8F0", backgroundColor: "#FFFFFF", color: "#64748B", fontSize: 13, fontWeight: 600, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            Cancel
          </button>
          <button onClick={handleConfirm}
            style={{ padding: "9px 22px", borderRadius: 9, border: "none", backgroundColor: "#1E3A5F", color: "#FFFFFF", fontSize: 13, fontWeight: 700, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            Confirm Adjustment
          </button>
        </div>
      </div>
    </div>
  );
}
