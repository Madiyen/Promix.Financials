import React, { useState, useMemo } from "react";
import {
  Search, Plus, Download, Filter, ChevronDown, Eye, Send,
  MoreHorizontal, ArrowUpRight, ArrowDownRight, FileText,
  DollarSign, AlertTriangle, CheckCircle, X, Trash2, Clock,
} from "lucide-react";

// ─── Types ─────────────────────────────────────────────────────────────────

type InvoiceStatus = "draft" | "sent" | "paid" | "overdue" | "partial";

interface LineItem {
  id: string;
  description: string;
  qty: number;
  unitPrice: number;
}

interface Invoice {
  id: string;
  customer: string;
  issueDate: string;
  dueDate: string;
  amount: number;
  paid: number;
  status: InvoiceStatus;
  agingDays: number; // positive = overdue by N days, negative = due in N days
}

// ─── Seed Data ──────────────────────────────────────────────────────────────

const SEED_INVOICES: Invoice[] = [
  { id: "INV-2026-001", customer: "TechCorp Solutions",   issueDate: "Jan 05, 2026", dueDate: "Feb 04, 2026", amount: 3200,  paid: 3200, status: "paid",    agingDays: 0   },
  { id: "INV-2026-002", customer: "Al-Farhan Trading",    issueDate: "Jan 10, 2026", dueDate: "Feb 09, 2026", amount: 7800,  paid: 7800, status: "paid",    agingDays: 0   },
  { id: "INV-2026-003", customer: "Orion Healthcare",     issueDate: "Oct 15, 2025", dueDate: "Nov 14, 2025", amount: 1250,  paid: 0,    status: "overdue", agingDays: 119 },
  { id: "INV-2026-004", customer: "NextGen Supplies",     issueDate: "Dec 05, 2025", dueDate: "Jan 04, 2026", amount: 4500,  paid: 0,    status: "overdue", agingDays: 68  },
  { id: "INV-2026-005", customer: "Summit Builders",      issueDate: "Jan 12, 2026", dueDate: "Feb 11, 2026", amount: 2100,  paid: 0,    status: "overdue", agingDays: 30  },
  { id: "INV-2026-006", customer: "Global Imports LLC",   issueDate: "Feb 01, 2026", dueDate: "Mar 03, 2026", amount: 6800,  paid: 0,    status: "overdue", agingDays: 10  },
  { id: "INV-2026-007", customer: "Sunrise Retail Co.",   issueDate: "Feb 15, 2026", dueDate: "Mar 31, 2026", amount: 12400, paid: 0,    status: "sent",    agingDays: -18 },
  { id: "INV-2026-008", customer: "TechCorp Solutions",   issueDate: "Feb 20, 2026", dueDate: "Mar 20, 2026", amount: 3600,  paid: 0,    status: "sent",    agingDays: -7  },
  { id: "INV-2026-009", customer: "Al-Farhan Trading",    issueDate: "Mar 03, 2026", dueDate: "Apr 02, 2026", amount: 890,   paid: 0,    status: "draft",   agingDays: -20 },
  { id: "INV-2026-010", customer: "NextGen Supplies",     issueDate: "Mar 08, 2026", dueDate: "Apr 07, 2026", amount: 2200,  paid: 0,    status: "draft",   agingDays: -25 },
  { id: "INV-2026-011", customer: "Global Imports LLC",   issueDate: "Jan 20, 2026", dueDate: "Feb 19, 2026", amount: 8500,  paid: 3000, status: "partial", agingDays: 22  },
  { id: "INV-2026-012", customer: "Sunrise Retail Co.",   issueDate: "Feb 05, 2026", dueDate: "Mar 07, 2026", amount: 1100,  paid: 1100, status: "paid",    agingDays: 0   },
  { id: "INV-2026-013", customer: "Summit Builders",      issueDate: "Feb 25, 2026", dueDate: "Mar 27, 2026", amount: 4900,  paid: 0,    status: "sent",    agingDays: -14 },
  { id: "INV-2026-014", customer: "TechCorp Solutions",   issueDate: "Feb 12, 2026", dueDate: "Mar 13, 2026", amount: 2750,  paid: 0,    status: "overdue", agingDays: 0   },
  { id: "INV-2026-015", customer: "Al-Farhan Trading",    issueDate: "Feb 08, 2026", dueDate: "Mar 10, 2026", amount: 6200,  paid: 2000, status: "partial", agingDays: 3   },
];

const RECENT_PAYMENTS = [
  { invoiceId: "INV-2026-001", customer: "TechCorp Solutions",  amount: 3200, date: "Mar 02, 2026", method: "Bank Transfer" },
  { invoiceId: "INV-2026-002", customer: "Al-Farhan Trading",   amount: 7800, date: "Mar 05, 2026", method: "Cheque"        },
  { invoiceId: "INV-2026-012", customer: "Sunrise Retail Co.",  amount: 1100, date: "Mar 08, 2026", method: "Bank Transfer" },
  { invoiceId: "INV-2026-015", customer: "Al-Farhan Trading",   amount: 2000, date: "Mar 10, 2026", method: "Cash"          },
];

// ─── Config ────────────────────────────────────────────────────────────────

const STATUS_CFG: Record<InvoiceStatus, { label: string; color: string; bg: string; dot: string }> = {
  draft:   { label: "Draft",   color: "#64748B", bg: "#F8FAFC", dot: "#94A3B8" },
  sent:    { label: "Sent",    color: "#1D4ED8", bg: "#EFF6FF", dot: "#3B82F6" },
  paid:    { label: "Paid",    color: "#065F46", bg: "#ECFDF5", dot: "#10B981" },
  overdue: { label: "Overdue", color: "#991B1B", bg: "#FEF2F2", dot: "#EF4444" },
  partial: { label: "Partial", color: "#92400E", bg: "#FFFBEB", dot: "#F59E0B" },
};

const FILTER_TABS: { key: string; label: string }[] = [
  { key: "All",     label: "All" },
  { key: "draft",   label: "Draft" },
  { key: "sent",    label: "Sent" },
  { key: "overdue", label: "Overdue" },
  { key: "partial", label: "Partial" },
  { key: "paid",    label: "Paid" },
];

const CUSTOMERS = ["TechCorp Solutions", "Al-Farhan Trading", "NextGen Supplies", "Global Imports LLC", "Summit Builders", "Orion Healthcare", "Sunrise Retail Co."];

// ─── Sub-components ────────────────────────────────────────────────────────

function StatusBadge({ status }: { status: InvoiceStatus }) {
  const cfg = STATUS_CFG[status];
  return (
    <span style={{ display: "inline-flex", alignItems: "center", gap: 4, backgroundColor: cfg.bg, color: cfg.color, fontSize: "10.5px", fontWeight: 600, padding: "2px 8px", borderRadius: 20, whiteSpace: "nowrap" }}>
      <span style={{ width: 5, height: 5, borderRadius: "50%", backgroundColor: cfg.dot, display: "inline-block", flexShrink: 0 }} />
      {cfg.label}
    </span>
  );
}

function AgingChip({ days, status }: { days: number; status: InvoiceStatus }) {
  if (status === "paid" || status === "draft") return null;
  if (days < 0) return <span style={{ fontSize: 10, color: "#3B82F6" }}>Due in {Math.abs(days)}d</span>;
  if (days === 0) return <span style={{ fontSize: 10, color: "#F59E0B", fontWeight: 700 }}>Due today</span>;
  return <span style={{ fontSize: 10, color: "#EF4444", fontWeight: 700 }}>{days}d overdue</span>;
}

// ─── New Invoice Modal ──────────────────────────────────────────────────────

interface NewInvoiceModalProps {
  nextId: string;
  onClose: () => void;
  onSave: (invoice: Invoice, asDraft: boolean) => void;
}

function NewInvoiceModal({ nextId, onClose, onSave }: NewInvoiceModalProps) {
  const [customer, setCustomer]   = useState("");
  const [issueDate, setIssueDate] = useState("Mar 13, 2026");
  const [dueDate, setDueDate]     = useState("Apr 12, 2026");
  const [notes, setNotes]         = useState("");
  const [lines, setLines]         = useState<LineItem[]>([
    { id: "l1", description: "", qty: 1, unitPrice: 0 },
    { id: "l2", description: "", qty: 1, unitPrice: 0 },
  ]);

  const subtotal = lines.reduce((s, l) => s + l.qty * l.unitPrice, 0);
  const tax      = subtotal * 0.05;
  const total    = subtotal + tax;

  const updateLine = (id: string, field: keyof LineItem, val: string | number) => {
    setLines(prev => prev.map(l => l.id === id ? { ...l, [field]: val } : l));
  };

  const addLine = () => {
    if (lines.length < 6) setLines(prev => [...prev, { id: `l${Date.now()}`, description: "", qty: 1, unitPrice: 0 }]);
  };

  const removeLine = (id: string) => {
    if (lines.length > 1) setLines(prev => prev.filter(l => l.id !== id));
  };

  const handleSave = (asDraft: boolean) => {
    const invoice: Invoice = {
      id: nextId,
      customer: customer || "New Customer",
      issueDate,
      dueDate,
      amount: total,
      paid: 0,
      status: asDraft ? "draft" : "sent",
      agingDays: -30,
    };
    onSave(invoice, asDraft);
  };

  return (
    <div onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
      style={{ position: "fixed", inset: 0, backgroundColor: "rgba(15,23,42,0.6)", backdropFilter: "blur(3px)", zIndex: 100, display: "flex", alignItems: "center", justifyContent: "center", fontFamily: "'Inter', sans-serif" }}>
      <div style={{ width: 720, maxHeight: "90vh", backgroundColor: "#FFFFFF", borderRadius: 16, boxShadow: "0 32px 80px rgba(0,0,0,0.28)", display: "flex", flexDirection: "column", overflow: "hidden" }}>

        {/* Header */}
        <div style={{ padding: "20px 24px 16px", borderBottom: "1px solid #F1F5F9", display: "flex", alignItems: "center", justifyContent: "space-between", flexShrink: 0 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
            <div style={{ width: 38, height: 38, borderRadius: 9, backgroundColor: "#EFF6FF", display: "flex", alignItems: "center", justifyContent: "center" }}>
              <FileText size={18} style={{ color: "#3B82F6" }} />
            </div>
            <div>
              <div style={{ color: "#0F172A", fontSize: 15, fontWeight: 800 }}>New Invoice</div>
              <div style={{ color: "#94A3B8", fontSize: 11, marginTop: 1 }}>Invoice #{nextId}</div>
            </div>
          </div>
          <button onClick={onClose} style={{ width: 32, height: 32, borderRadius: 8, border: "none", backgroundColor: "#F1F5F9", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center" }}>
            <X size={15} style={{ color: "#64748B" }} />
          </button>
        </div>

        {/* Body */}
        <div style={{ flex: 1, overflowY: "auto", padding: "20px 24px" }}>

          {/* Customer + Dates row */}
          <div style={{ display: "flex", gap: 14, marginBottom: 16 }}>
            <div style={{ flex: 2 }}>
              <label style={{ color: "#374151", fontSize: 12, fontWeight: 600, display: "block", marginBottom: 7 }}>Customer <span style={{ color: "#EF4444" }}>*</span></label>
              <select value={customer} onChange={e => setCustomer(e.target.value)}
                style={{ width: "100%", padding: "9px 12px", borderRadius: 8, border: "1.5px solid #E2E8F0", fontSize: 12.5, color: customer ? "#334155" : "#94A3B8", backgroundColor: "#FFFFFF", outline: "none", fontFamily: "'Inter', sans-serif", cursor: "pointer" }}>
                <option value="">Select customer…</option>
                {CUSTOMERS.map(c => <option key={c} value={c}>{c}</option>)}
              </select>
            </div>
            <div style={{ flex: 1 }}>
              <label style={{ color: "#374151", fontSize: 12, fontWeight: 600, display: "block", marginBottom: 7 }}>Issue Date</label>
              <input type="text" value={issueDate} onChange={e => setIssueDate(e.target.value)}
                style={{ width: "100%", padding: "9px 12px", borderRadius: 8, border: "1.5px solid #E2E8F0", fontSize: 12.5, color: "#334155", outline: "none", fontFamily: "'Inter', sans-serif", boxSizing: "border-box" }} />
            </div>
            <div style={{ flex: 1 }}>
              <label style={{ color: "#374151", fontSize: 12, fontWeight: 600, display: "block", marginBottom: 7 }}>Due Date</label>
              <input type="text" value={dueDate} onChange={e => setDueDate(e.target.value)}
                style={{ width: "100%", padding: "9px 12px", borderRadius: 8, border: "1.5px solid #E2E8F0", fontSize: 12.5, color: "#334155", outline: "none", fontFamily: "'Inter', sans-serif", boxSizing: "border-box" }} />
            </div>
          </div>

          {/* Line Items */}
          <div style={{ marginBottom: 16 }}>
            <label style={{ color: "#374151", fontSize: 12, fontWeight: 600, display: "block", marginBottom: 8 }}>Line Items</label>
            <div style={{ border: "1.5px solid #E2E8F0", borderRadius: 9, overflow: "hidden" }}>
              {/* Header */}
              <div style={{ display: "grid", gridTemplateColumns: "1fr 80px 120px 100px 32px", gap: 0, backgroundColor: "#F8FAFC", padding: "8px 12px", borderBottom: "1px solid #E2E8F0" }}>
                {["Description", "Qty", "Unit Price", "Total", ""].map((h, i) => (
                  <div key={i} style={{ color: "#64748B", fontSize: 10.5, fontWeight: 700, textTransform: "uppercase", textAlign: h === "Total" ? "right" : "left" }}>{h}</div>
                ))}
              </div>
              {/* Rows */}
              {lines.map((line, idx) => {
                const lineTotal = line.qty * line.unitPrice;
                return (
                  <div key={line.id} style={{ display: "grid", gridTemplateColumns: "1fr 80px 120px 100px 32px", gap: 0, padding: "8px 12px", borderBottom: idx < lines.length - 1 ? "1px solid #F1F5F9" : "none", alignItems: "center" }}>
                    <input placeholder={`Item ${idx + 1} description`} value={line.description}
                      onChange={e => updateLine(line.id, "description", e.target.value)}
                      style={{ border: "none", outline: "none", fontSize: 12.5, color: "#334155", fontFamily: "'Inter', sans-serif", width: "100%", paddingRight: 8 }} />
                    <input type="number" min={1} value={line.qty}
                      onChange={e => updateLine(line.id, "qty", parseInt(e.target.value) || 0)}
                      style={{ border: "1px solid #E2E8F0", borderRadius: 6, padding: "4px 6px", width: "64px", fontSize: 12.5, textAlign: "center", outline: "none", fontFamily: "'Inter', sans-serif" }} />
                    <div style={{ display: "flex", alignItems: "center", gap: 4 }}>
                      <span style={{ color: "#94A3B8", fontSize: 12 }}>$</span>
                      <input type="number" min={0} value={line.unitPrice}
                        onChange={e => updateLine(line.id, "unitPrice", parseFloat(e.target.value) || 0)}
                        style={{ border: "1px solid #E2E8F0", borderRadius: 6, padding: "4px 6px", width: "80px", fontSize: 12.5, outline: "none", fontFamily: "'Inter', sans-serif" }} />
                    </div>
                    <div style={{ textAlign: "right", color: lineTotal > 0 ? "#0F172A" : "#94A3B8", fontSize: 13, fontWeight: lineTotal > 0 ? 700 : 400 }}>
                      ${lineTotal.toLocaleString("en-US", { minimumFractionDigits: 2 })}
                    </div>
                    <button onClick={() => removeLine(line.id)} style={{ width: 26, height: 26, borderRadius: 6, border: "none", backgroundColor: "transparent", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center" }}>
                      <Trash2 size={12} style={{ color: "#CBD5E1" }} />
                    </button>
                  </div>
                );
              })}
            </div>
            <button onClick={addLine} style={{ marginTop: 8, display: "flex", alignItems: "center", gap: 5, padding: "6px 12px", borderRadius: 7, border: "1.5px dashed #CBD5E1", backgroundColor: "transparent", color: "#64748B", fontSize: 12, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
              <Plus size={13} /> Add Line Item
            </button>
          </div>

          {/* Notes + Totals */}
          <div style={{ display: "flex", gap: 16, alignItems: "flex-start" }}>
            <div style={{ flex: 1 }}>
              <label style={{ color: "#374151", fontSize: 12, fontWeight: 600, display: "block", marginBottom: 7 }}>Notes</label>
              <textarea placeholder="Payment terms, thank you note, or other details…" value={notes}
                onChange={e => setNotes(e.target.value)} rows={3}
                style={{ width: "100%", padding: "9px 12px", borderRadius: 8, border: "1.5px solid #E2E8F0", fontSize: 12.5, color: "#334155", outline: "none", fontFamily: "'Inter', sans-serif", resize: "none", boxSizing: "border-box" }} />
            </div>
            <div style={{ width: 220, flexShrink: 0 }}>
              {[
                { label: "Subtotal", val: subtotal, bold: false },
                { label: "Tax (5%)",  val: tax,      bold: false },
              ].map(row => (
                <div key={row.label} style={{ display: "flex", justifyContent: "space-between", padding: "5px 0" }}>
                  <span style={{ color: "#64748B", fontSize: 12.5 }}>{row.label}</span>
                  <span style={{ color: "#334155", fontSize: 12.5 }}>${row.val.toLocaleString("en-US", { minimumFractionDigits: 2 })}</span>
                </div>
              ))}
              <div style={{ borderTop: "2px solid #E2E8F0", marginTop: 6, paddingTop: 8, display: "flex", justifyContent: "space-between" }}>
                <span style={{ color: "#0F172A", fontSize: 14, fontWeight: 800 }}>Total</span>
                <span style={{ color: "#1E3A5F", fontSize: 15, fontWeight: 800 }}>${total.toLocaleString("en-US", { minimumFractionDigits: 2 })}</span>
              </div>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div style={{ padding: "14px 24px", borderTop: "1px solid #F1F5F9", display: "flex", gap: 10, justifyContent: "flex-end", flexShrink: 0 }}>
          <button onClick={onClose} style={{ padding: "9px 20px", borderRadius: 9, border: "1.5px solid #E2E8F0", backgroundColor: "#FFFFFF", color: "#64748B", fontSize: 13, fontWeight: 600, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>Cancel</button>
          <button onClick={() => handleSave(true)} style={{ padding: "9px 18px", borderRadius: 9, border: "1.5px solid #1E3A5F", backgroundColor: "#FFFFFF", color: "#1E3A5F", fontSize: 13, fontWeight: 600, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>Save Draft</button>
          <button onClick={() => handleSave(false)} style={{ padding: "9px 22px", borderRadius: 9, border: "none", backgroundColor: "#1E3A5F", color: "#FFFFFF", fontSize: 13, fontWeight: 700, cursor: "pointer", fontFamily: "'Inter', sans-serif", display: "flex", alignItems: "center", gap: 7 }}>
            <Send size={13} /> Send Invoice
          </button>
        </div>
      </div>
    </div>
  );
}

// ─── Main Component ─────────────────────────────────────────────────────────

export function InvoicesTab() {
  const [invoices, setInvoices]         = useState<Invoice[]>(SEED_INVOICES);
  const [activeTab, setActiveTab]       = useState("All");
  const [search, setSearch]             = useState("");
  const [showNewModal, setShowNewModal] = useState(false);

  // ── Filtered list ──
  const filtered = useMemo(() => {
    return invoices.filter(inv => {
      const matchTab = activeTab === "All" || inv.status === activeTab;
      const q = search.toLowerCase();
      const matchSearch = !q || inv.id.toLowerCase().includes(q) || inv.customer.toLowerCase().includes(q);
      return matchTab && matchSearch;
    });
  }, [invoices, activeTab, search]);

  // ── KPIs ──
  const totalCount     = invoices.length;
  const outstanding    = invoices.filter(i => i.status !== "paid").reduce((s, i) => s + (i.amount - i.paid), 0);
  const overdueCount   = invoices.filter(i => i.status === "overdue").length;
  const overdueAmt     = invoices.filter(i => i.status === "overdue").reduce((s, i) => s + (i.amount - i.paid), 0);
  const collectedAmt   = RECENT_PAYMENTS.reduce((s, p) => s + p.amount, 0);

  const kpiCards = [
    { title: "Total Invoices",     value: `${totalCount}`,                                           change: "+3 this month",  positive: true,  color: "#3B82F6", bg: "#EFF6FF", icon: <FileText size={19} /> },
    { title: "Total Outstanding",  value: `$${(outstanding / 1000).toFixed(1)}k`,                   change: "10 invoices",    positive: false, color: "#F59E0B", bg: "#FFFBEB", icon: <Clock size={19} /> },
    { title: "Overdue Invoices",   value: `${overdueCount}`,                                         change: `$${(overdueAmt / 1000).toFixed(1)}k at risk`, positive: false, color: "#EF4444", bg: "#FEF2F2", icon: <AlertTriangle size={19} /> },
    { title: "Collected (Mar)",    value: `$${(collectedAmt / 1000).toFixed(1)}k`,                   change: "+22.3% vs Feb",  positive: true,  color: "#10B981", bg: "#ECFDF5", icon: <CheckCircle size={19} /> },
  ];

  // ── Aging schedule ──
  const agingBuckets = [
    { label: "Current (not due)",   days: "—",       amount: 20900, color: "#10B981", pct: 43.5 },
    { label: "0–30 days overdue",   days: "0–30",    amount: 13750, color: "#F59E0B", pct: 28.6 },
    { label: "31–60 days overdue",  days: "31–60",   amount: 7600,  color: "#F97316", pct: 15.8 },
    { label: "61–90 days overdue",  days: "61–90",   amount: 4500,  color: "#EF4444", pct: 9.4  },
    { label: "90+ days overdue",    days: "90+",     amount: 1250,  color: "#991B1B", pct: 2.6  },
  ];
  const totalAging = agingBuckets.reduce((s, b) => s + b.amount, 0);

  const nextId = `INV-2026-0${(invoices.length + 1).toString().padStart(2, "0")}`;

  const handleSaveInvoice = (invoice: Invoice) => {
    setInvoices(prev => [invoice, ...prev]);
    setShowNewModal(false);
  };

  const tabCount = (key: string) => key === "All" ? invoices.length : invoices.filter(i => i.status === key).length;

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 16, fontFamily: "'Inter', sans-serif" }}>

      {/* ── Page header ── */}
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div>
          <h2 style={{ margin: 0, color: "#0F172A", fontSize: 16, fontWeight: 800 }}>Invoices</h2>
          <p style={{ margin: "2px 0 0", color: "#94A3B8", fontSize: 12 }}>Manage sales invoices, aging schedule & payment tracking · FY 2025–26</p>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <button style={{ display: "flex", alignItems: "center", gap: 6, padding: "8px 14px", borderRadius: 9, border: "1px solid #E2E8F0", backgroundColor: "#FFFFFF", color: "#64748B", fontSize: 12, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <Download size={13} /> Export
          </button>
          <button onClick={() => setShowNewModal(true)}
            style={{ display: "flex", alignItems: "center", gap: 6, padding: "8px 16px", borderRadius: 9, border: "none", backgroundColor: "#1E3A5F", color: "#FFFFFF", fontSize: 12, fontWeight: 700, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <Plus size={14} /> New Invoice
          </button>
        </div>
      </div>

      {/* ── KPI Cards ── */}
      <div style={{ display: "flex", gap: 14 }}>
        {kpiCards.map(card => (
          <div key={card.title} style={{ flex: 1, backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: 12, padding: "15px 18px", boxShadow: "0 1px 4px rgba(0,0,0,0.05)", display: "flex", alignItems: "center", gap: 13, position: "relative", overflow: "hidden" }}>
            <div style={{ position: "absolute", top: 0, left: 0, right: 0, height: 3, backgroundColor: card.color, borderRadius: "12px 12px 0 0" }} />
            <div style={{ width: 40, height: 40, borderRadius: 10, backgroundColor: card.bg, display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
              <span style={{ color: card.color }}>{card.icon}</span>
            </div>
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ color: "#64748B", fontSize: 11, fontWeight: 500, marginBottom: 3 }}>{card.title}</div>
              <div style={{ color: "#0F172A", fontSize: 20, fontWeight: 800, letterSpacing: "-0.4px", lineHeight: 1 }}>{card.value}</div>
            </div>
            <span style={{ display: "inline-flex", alignItems: "center", gap: 3, backgroundColor: card.positive ? "#ECFDF5" : "#FEF2F2", color: card.positive ? "#10B981" : "#EF4444", fontSize: 10, fontWeight: 700, padding: "3px 7px", borderRadius: 20, flexShrink: 0 }}>
              {card.positive ? <ArrowUpRight size={10} /> : <ArrowDownRight size={10} />}
              {card.change}
            </span>
          </div>
        ))}
      </div>

      {/* ── Main: Table + Right Panel ── */}
      <div style={{ display: "flex", gap: 16, alignItems: "flex-start" }}>

        {/* ── Invoice Table ── */}
        <div style={{ flex: 1, backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: 12, boxShadow: "0 1px 4px rgba(0,0,0,0.05)", overflow: "hidden", minWidth: 0 }}>

          {/* Filter bar */}
          <div style={{ padding: "13px 18px", borderBottom: "1px solid #F1F5F9", display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12 }}>
            <div style={{ display: "flex", gap: 3, backgroundColor: "#F5F7FA", padding: 3, borderRadius: 10, flexShrink: 0 }}>
              {FILTER_TABS.map(tab => {
                const isActive = activeTab === tab.key;
                const cfg = tab.key !== "All" ? STATUS_CFG[tab.key as InvoiceStatus] : null;
                return (
                  <button key={tab.key} onClick={() => setActiveTab(tab.key)}
                    style={{ padding: "4px 10px", borderRadius: 7, border: "none", backgroundColor: isActive ? "#FFFFFF" : "transparent", color: isActive ? (cfg?.color ?? "#1E3A5F") : "#64748B", fontSize: 11.5, fontWeight: isActive ? 700 : 400, cursor: "pointer", boxShadow: isActive ? "0 1px 3px rgba(0,0,0,0.1)" : "none", display: "flex", alignItems: "center", gap: 4, fontFamily: "'Inter', sans-serif", whiteSpace: "nowrap" }}>
                    {tab.label}
                    <span style={{ backgroundColor: isActive ? (cfg?.bg ?? "#EFF6FF") : "#E2E8F0", color: isActive ? (cfg?.color ?? "#1E3A5F") : "#94A3B8", fontSize: 9.5, fontWeight: 700, padding: "1px 5px", borderRadius: 8 }}>
                      {tabCount(tab.key)}
                    </span>
                  </button>
                );
              })}
            </div>
            <div style={{ display: "flex", gap: 8 }}>
              <div style={{ display: "flex", alignItems: "center", gap: 7, padding: "6px 12px", borderRadius: 8, border: "1px solid #E2E8F0", backgroundColor: "#F8FAFC", width: 180 }}>
                <Search size={12} style={{ color: "#94A3B8", flexShrink: 0 }} />
                <input type="text" placeholder="Search invoices…" value={search} onChange={e => setSearch(e.target.value)}
                  style={{ flex: 1, border: "none", outline: "none", background: "transparent", color: "#334155", fontSize: 12, fontFamily: "'Inter', sans-serif" }} />
              </div>
              <button style={{ display: "flex", alignItems: "center", gap: 5, padding: "6px 11px", borderRadius: 8, border: "1px solid #E2E8F0", backgroundColor: "#F8FAFC", color: "#64748B", fontSize: 11.5, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
                <Filter size={12} /> Filter <ChevronDown size={11} />
              </button>
            </div>
          </div>

          {/* Table */}
          <div style={{ overflowY: "auto", maxHeight: 370 }}>
            <table style={{ width: "100%", borderCollapse: "collapse" }}>
              <thead style={{ position: "sticky", top: 0, zIndex: 2 }}>
                <tr style={{ backgroundColor: "#F8FAFC" }}>
                  {["Invoice #", "Customer", "Issued", "Due Date", "Amount", "Paid", "Balance", "Status", ""].map(h => (
                    <th key={h} style={{ padding: "8px 13px", textAlign: ["Amount", "Paid", "Balance"].includes(h) ? "right" : "left", color: "#64748B", fontSize: 10.5, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.5px", borderBottom: "1px solid #E2E8F0", whiteSpace: "nowrap", backgroundColor: "#F8FAFC" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {filtered.map((inv, idx) => {
                  const balance = inv.amount - inv.paid;
                  return (
                    <tr key={inv.id}
                      style={{ backgroundColor: idx % 2 === 0 ? "#FFFFFF" : "#FAFBFC", borderBottom: "1px solid #F1F5F9" }}
                      onMouseEnter={e => { (e.currentTarget as HTMLTableRowElement).style.backgroundColor = "#F0F7FF"; }}
                      onMouseLeave={e => { (e.currentTarget as HTMLTableRowElement).style.backgroundColor = idx % 2 === 0 ? "#FFFFFF" : "#FAFBFC"; }}>
                      <td style={{ padding: "10px 13px" }}>
                        <span style={{ color: "#3B82F6", fontSize: 11.5, fontWeight: 700, fontFamily: "monospace" }}>{inv.id}</span>
                      </td>
                      <td style={{ padding: "10px 13px" }}>
                        <div>
                          <div style={{ color: "#0F172A", fontSize: 12.5, fontWeight: 600, whiteSpace: "nowrap" }}>{inv.customer}</div>
                        </div>
                      </td>
                      <td style={{ padding: "10px 13px", color: "#64748B", fontSize: 12, whiteSpace: "nowrap" }}>{inv.issueDate}</td>
                      <td style={{ padding: "10px 13px", whiteSpace: "nowrap" }}>
                        <div style={{ color: "#334155", fontSize: 12 }}>{inv.dueDate}</div>
                        <AgingChip days={inv.agingDays} status={inv.status} />
                      </td>
                      <td style={{ padding: "10px 13px", textAlign: "right", color: "#0F172A", fontSize: 13, fontWeight: 700 }}>
                        ${inv.amount.toLocaleString("en-US", { minimumFractionDigits: 2 })}
                      </td>
                      <td style={{ padding: "10px 13px", textAlign: "right", color: inv.paid > 0 ? "#10B981" : "#94A3B8", fontSize: 12.5, fontWeight: 600 }}>
                        ${inv.paid.toLocaleString("en-US", { minimumFractionDigits: 2 })}
                      </td>
                      <td style={{ padding: "10px 13px", textAlign: "right", color: balance > 0 ? (inv.status === "overdue" ? "#EF4444" : "#0F172A") : "#10B981", fontSize: 13, fontWeight: 700 }}>
                        ${balance.toLocaleString("en-US", { minimumFractionDigits: 2 })}
                      </td>
                      <td style={{ padding: "10px 13px" }}><StatusBadge status={inv.status} /></td>
                      <td style={{ padding: "10px 13px" }}>
                        <div style={{ display: "flex", gap: 4 }}>
                          <button title="View" style={{ width: 26, height: 26, borderRadius: 7, border: "none", backgroundColor: "#EFF6FF", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center" }}>
                            <Eye size={11} style={{ color: "#3B82F6" }} />
                          </button>
                          {inv.status === "draft" && (
                            <button title="Send" style={{ width: 26, height: 26, borderRadius: 7, border: "none", backgroundColor: "#ECFDF5", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center" }}>
                              <Send size={11} style={{ color: "#10B981" }} />
                            </button>
                          )}
                          <button title="More" style={{ width: 26, height: 26, borderRadius: 7, border: "none", backgroundColor: "#F8FAFC", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center" }}>
                            <MoreHorizontal size={11} style={{ color: "#94A3B8" }} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
            {filtered.length === 0 && (
              <div style={{ padding: 40, textAlign: "center" }}>
                <FileText size={30} style={{ color: "#CBD5E1", margin: "0 auto 10px" }} />
                <p style={{ color: "#94A3B8", fontSize: 13, margin: 0 }}>No invoices match your filter.</p>
              </div>
            )}
          </div>

          {/* Footer */}
          <div style={{ padding: "10px 18px", borderTop: "1px solid #F1F5F9", display: "flex", alignItems: "center", justifyContent: "space-between" }}>
            <span style={{ color: "#94A3B8", fontSize: 12 }}>Showing {filtered.length} of {invoices.length} invoices</span>
            <div style={{ display: "flex", gap: 4 }}>
              {["‹", "1", "2", "›"].map((p, i) => (
                <button key={i} style={{ minWidth: 28, height: 28, borderRadius: 7, border: p === "1" ? "none" : "1px solid #E2E8F0", backgroundColor: p === "1" ? "#1E3A5F" : "transparent", color: p === "1" ? "#FFFFFF" : "#64748B", fontSize: 12, fontWeight: p === "1" ? 700 : 400, cursor: "pointer", padding: "0 8px", fontFamily: "'Inter', sans-serif" }}>
                  {p}
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* ── Right Panel ── */}
        <div style={{ width: 300, minWidth: 300, display: "flex", flexDirection: "column", gap: 14 }}>

          {/* Aging Schedule */}
          <div style={{ backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: 12, padding: 18, boxShadow: "0 1px 4px rgba(0,0,0,0.05)" }}>
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 14 }}>
              <h4 style={{ margin: 0, color: "#0F172A", fontSize: 13, fontWeight: 700 }}>Aging Schedule</h4>
              <span style={{ color: "#94A3B8", fontSize: 11 }}>${(totalAging / 1000).toFixed(0)}k total</span>
            </div>
            {/* Stacked bar */}
            <div style={{ height: 10, borderRadius: 6, overflow: "hidden", display: "flex", marginBottom: 14, gap: 2 }}>
              {agingBuckets.map((b, i) => (
                <div key={i} style={{ height: "100%", width: `${b.pct}%`, backgroundColor: b.color, borderRadius: 3 }} />
              ))}
            </div>
            {/* Rows */}
            <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
              {agingBuckets.map((b, i) => (
                <div key={i} style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                  <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                    <div style={{ width: 8, height: 8, borderRadius: 2, backgroundColor: b.color, flexShrink: 0 }} />
                    <span style={{ color: "#64748B", fontSize: 11 }}>{b.label}</span>
                  </div>
                  <div style={{ textAlign: "right" }}>
                    <span style={{ color: "#0F172A", fontSize: 12, fontWeight: 700 }}>${b.amount.toLocaleString()}</span>
                    <span style={{ color: "#94A3B8", fontSize: 10, marginLeft: 5 }}>({b.pct}%)</span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Recent Payments */}
          <div style={{ backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: 12, padding: 18, boxShadow: "0 1px 4px rgba(0,0,0,0.05)" }}>
            <h4 style={{ margin: "0 0 12px", color: "#0F172A", fontSize: 13, fontWeight: 700 }}>Recent Payments</h4>
            <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
              {RECENT_PAYMENTS.map((p, i) => (
                <div key={i} style={{ display: "flex", alignItems: "center", gap: 10, padding: "9px 10px", borderRadius: 9, backgroundColor: "#F8FAFC", border: "1px solid #F1F5F9" }}>
                  <div style={{ width: 30, height: 30, borderRadius: 8, backgroundColor: "#ECFDF5", display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
                    <CheckCircle size={14} style={{ color: "#10B981" }} />
                  </div>
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ color: "#0F172A", fontSize: 11.5, fontWeight: 600, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{p.customer}</div>
                    <div style={{ color: "#94A3B8", fontSize: 10, marginTop: 1 }}>{p.invoiceId} · {p.date} · {p.method}</div>
                  </div>
                  <span style={{ color: "#10B981", fontSize: 12.5, fontWeight: 800, flexShrink: 0 }}>+${p.amount.toLocaleString()}</span>
                </div>
              ))}
            </div>
          </div>

          {/* Summary card */}
          <div style={{ backgroundColor: "#1E3A5F", borderRadius: 12, padding: 18, boxShadow: "0 4px 16px rgba(30,58,95,0.25)" }}>
            <h4 style={{ margin: "0 0 14px", color: "#FFFFFF", fontSize: 13, fontWeight: 700 }}>AR Summary</h4>
            {[
              { label: "Total Invoiced",    value: `$${invoices.reduce((s,i)=>s+i.amount,0).toLocaleString()}`, color: "#FFFFFF" },
              { label: "Total Collected",   value: `$${invoices.reduce((s,i)=>s+i.paid,0).toLocaleString()}`,   color: "#10B981" },
              { label: "Outstanding",       value: `$${outstanding.toLocaleString()}`,                           color: "#F59E0B" },
              { label: "Overdue",           value: `$${overdueAmt.toLocaleString()}`,                            color: "#EF4444" },
            ].map(row => (
              <div key={row.label} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", paddingBottom: 8, marginBottom: 8, borderBottom: "1px solid rgba(255,255,255,0.08)" }}>
                <span style={{ color: "rgba(255,255,255,0.55)", fontSize: 11.5 }}>{row.label}</span>
                <span style={{ color: row.color, fontSize: 13, fontWeight: 700 }}>{row.value}</span>
              </div>
            ))}
            <div style={{ marginTop: 4 }}>
              <div style={{ color: "rgba(255,255,255,0.45)", fontSize: 10, marginBottom: 5 }}>Collection Rate</div>
              <div style={{ height: 5, backgroundColor: "rgba(255,255,255,0.12)", borderRadius: 3 }}>
                <div style={{ height: "100%", width: `${Math.round(invoices.reduce((s,i)=>s+i.paid,0)/invoices.reduce((s,i)=>s+i.amount,0)*100)}%`, backgroundColor: "#10B981", borderRadius: 3 }} />
              </div>
              <div style={{ color: "rgba(255,255,255,0.5)", fontSize: 10, marginTop: 4 }}>
                {Math.round(invoices.reduce((s,i)=>s+i.paid,0)/invoices.reduce((s,i)=>s+i.amount,0)*100)}% collected
              </div>
            </div>
          </div>

        </div>
      </div>

      {showNewModal && (
        <NewInvoiceModal nextId={nextId} onClose={() => setShowNewModal(false)} onSave={handleSaveInvoice} />
      )}
    </div>
  );
}
