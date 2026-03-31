import React, { useState, useMemo } from "react";
import {
  Search, Plus, Download, Filter, ChevronDown, X, Printer,
  CheckCircle, XCircle, Clock, ArrowUpRight, ArrowDownLeft,
  DollarSign, Banknote, CreditCard, FileText, Eye, Stamp,
  ArrowDownRight, ArrowUpCircle, Trash2,
} from "lucide-react";

// ─── Types ──────────────────────────────────────────────────────────────────

type VoucherType   = "receipt" | "payment";
type VoucherStatus = "draft" | "approved" | "cancelled";
type PayMethod     = "Cash" | "Bank Transfer" | "Cheque" | "Card";

interface Voucher {
  id: string;
  type: VoucherType;
  date: string;
  beneficiary: string;
  description: string;
  amount: number;
  method: PayMethod;
  account: string;
  accountCode: string;
  status: VoucherStatus;
  ref?: string;
  approvedBy?: string;
}

// ─── Seed Data ───────────────────────────────────────────────────────────────

const SEED: Voucher[] = [
  { id: "RV-2026-001", type: "receipt",  date: "Mar 01, 2026", beneficiary: "TechCorp Solutions",  description: "Payment received for INV-2026-001",        amount: 3200,  method: "Bank Transfer", account: "Cash & Bank — Main",   accountCode: "1101", status: "approved", ref: "INV-2026-001", approvedBy: "Ahmed Hassan" },
  { id: "RV-2026-002", type: "receipt",  date: "Mar 05, 2026", beneficiary: "Al-Farhan Trading",   description: "Cheque collection — INV-2026-002",         amount: 7800,  method: "Cheque",        account: "Cash & Bank — Main",   accountCode: "1101", status: "approved", ref: "INV-2026-002", approvedBy: "Ahmed Hassan" },
  { id: "PV-2026-001", type: "payment",  date: "Mar 02, 2026", beneficiary: "NextGen Supplies",    description: "Settlement of PO-2026-055 (partial)",      amount: 6500,  method: "Bank Transfer", account: "Accounts Payable",     accountCode: "2101", status: "approved", ref: "PO-2026-055", approvedBy: "Ahmed Hassan" },
  { id: "PV-2026-002", type: "payment",  date: "Mar 04, 2026", beneficiary: "Utility Co.",         description: "Electricity & water bill — February",      amount: 1240,  method: "Cheque",        account: "Utilities Expense",    accountCode: "6201", status: "approved", ref: "UTIL-0226",   approvedBy: "Sara Al-Rashid" },
  { id: "RV-2026-003", type: "receipt",  date: "Mar 08, 2026", beneficiary: "Sunrise Retail Co.",  description: "Cash sale collection",                     amount: 1100,  method: "Cash",          account: "Petty Cash",           accountCode: "1102", status: "approved", ref: "INV-2026-012", approvedBy: "Ahmed Hassan" },
  { id: "PV-2026-003", type: "payment",  date: "Mar 10, 2026", beneficiary: "Global Imports LLC",  description: "Advance payment — PO-2026-057",            amount: 14200, method: "Bank Transfer", account: "Accounts Payable",     accountCode: "2101", status: "approved", ref: "PO-2026-057", approvedBy: "Sara Al-Rashid" },
  { id: "PV-2026-004", type: "payment",  date: "Mar 11, 2026", beneficiary: "Al-Farhan Trading",   description: "Purchase of office supplies",              amount: 3400,  method: "Cheque",        account: "Office Expense",       accountCode: "6101", status: "approved", ref: "PO-2026-059", approvedBy: "Ahmed Hassan" },
  { id: "RV-2026-004", type: "receipt",  date: "Mar 10, 2026", beneficiary: "Al-Farhan Trading",   description: "Partial payment — INV-2026-015",           amount: 2000,  method: "Cash",          account: "Petty Cash",           accountCode: "1102", status: "approved", ref: "INV-2026-015", approvedBy: "Ahmed Hassan" },
  { id: "PV-2026-005", type: "payment",  date: "Mar 12, 2026", beneficiary: "Office Rent LLC",     description: "March 2026 office rent",                   amount: 8500,  method: "Bank Transfer", account: "Rent Expense",         accountCode: "6102", status: "approved", ref: "RENT-0326",   approvedBy: "Sara Al-Rashid" },
  { id: "RV-2026-005", type: "receipt",  date: "Mar 14, 2026", beneficiary: "Summit Builders",     description: "Down payment — INV-2026-013",              amount: 2000,  method: "Cheque",        account: "Cash & Bank — Main",   accountCode: "1101", status: "draft",    ref: "INV-2026-013" },
  { id: "PV-2026-006", type: "payment",  date: "Mar 15, 2026", beneficiary: "Summit Builders",     description: "Freight & delivery charges",               amount: 680,   method: "Cash",          account: "Transport Expense",    accountCode: "6301", status: "draft",    ref: "SHIP-0326" },
  { id: "RV-2026-006", type: "receipt",  date: "Mar 16, 2026", beneficiary: "Orion Healthcare",    description: "Refund received — returned goods",         amount: 450,   method: "Bank Transfer", account: "Cash & Bank — Main",   accountCode: "1101", status: "draft",    ref: "RFD-0326" },
  { id: "PV-2026-007", type: "payment",  date: "Mar 08, 2026", beneficiary: "Insurance Co.",       description: "Annual insurance premium",                 amount: 4800,  method: "Cheque",        account: "Insurance Expense",    accountCode: "6401", status: "cancelled", ref: "INS-0326" },
  { id: "RV-2026-007", type: "receipt",  date: "Mar 03, 2026", beneficiary: "Global Imports LLC",  description: "Security deposit returned",                amount: 5000,  method: "Bank Transfer", account: "Cash & Bank — Main",   accountCode: "1101", status: "cancelled", ref: "DEP-2025" },
];

const ACCOUNTS = [
  { code: "1101", name: "Cash & Bank — Main"  },
  { code: "1102", name: "Petty Cash"           },
  { code: "1201", name: "Accounts Receivable"  },
  { code: "2101", name: "Accounts Payable"     },
  { code: "6101", name: "Office Expense"       },
  { code: "6102", name: "Rent Expense"         },
  { code: "6201", name: "Utilities Expense"    },
  { code: "6301", name: "Transport Expense"    },
  { code: "6401", name: "Insurance Expense"    },
];

const BENEFICIARIES = [
  "TechCorp Solutions", "Al-Farhan Trading", "NextGen Supplies",
  "Global Imports LLC", "Summit Builders", "Orion Healthcare",
  "Sunrise Retail Co.", "Utility Co.", "Office Rent LLC", "Insurance Co.",
];

// ─── Config ──────────────────────────────────────────────────────────────────

const STATUS_CFG: Record<VoucherStatus, { label: string; color: string; bg: string; dot: string; border: string }> = {
  draft:     { label: "Draft",     color: "#4B5563", bg: "#F9FAFB", dot: "#9CA3AF", border: "#E5E7EB" },
  approved:  { label: "Approved",  color: "#065F46", bg: "#ECFDF5", dot: "#10B981", border: "#A7F3D0" },
  cancelled: { label: "Cancelled", color: "#991B1B", bg: "#FEF2F2", dot: "#EF4444", border: "#FECACA" },
};

const METHOD_CFG: Record<PayMethod, { icon: React.ReactNode; color: string; bg: string }> = {
  "Cash":          { icon: <Banknote size={11} />,   color: "#065F46", bg: "#ECFDF5" },
  "Bank Transfer": { icon: <ArrowUpCircle size={11}/>,color: "#1D4ED8", bg: "#EFF6FF" },
  "Cheque":        { icon: <FileText size={11} />,    color: "#92400E", bg: "#FFFBEB" },
  "Card":          { icon: <CreditCard size={11} />,  color: "#6D28D9", bg: "#F5F3FF" },
};

const FILTER_TABS = [
  { key: "All",     label: "All Vouchers" },
  { key: "receipt", label: "Receipt Vouchers" },
  { key: "payment", label: "Payment Vouchers" },
];

// ─── Sub-components ──────────────────────────────────────────────────────────

function StatusBadge({ status }: { status: VoucherStatus }) {
  const c = STATUS_CFG[status];
  return (
    <span style={{ display: "inline-flex", alignItems: "center", gap: 4, backgroundColor: c.bg, color: c.color, fontSize: "10.5px", fontWeight: 600, padding: "2px 8px", borderRadius: 20, border: `1px solid ${c.border}`, whiteSpace: "nowrap" }}>
      <span style={{ width: 5, height: 5, borderRadius: "50%", backgroundColor: c.dot, flexShrink: 0 }} />
      {c.label}
    </span>
  );
}

function TypeBadge({ type }: { type: VoucherType }) {
  const isReceipt = type === "receipt";
  return (
    <span style={{ display: "inline-flex", alignItems: "center", gap: 4, backgroundColor: isReceipt ? "#ECFDF5" : "#FEF2F2", color: isReceipt ? "#065F46" : "#991B1B", fontSize: "10.5px", fontWeight: 700, padding: "2px 8px", borderRadius: 20, whiteSpace: "nowrap" }}>
      {isReceipt ? <ArrowDownLeft size={10} /> : <ArrowUpRight size={10} />}
      {isReceipt ? "Receipt" : "Payment"}
    </span>
  );
}

function MethodBadge({ method }: { method: PayMethod }) {
  const c = METHOD_CFG[method];
  return (
    <span style={{ display: "inline-flex", alignItems: "center", gap: 4, backgroundColor: c.bg, color: c.color, fontSize: "10px", fontWeight: 600, padding: "2px 7px", borderRadius: 8 }}>
      <span style={{ color: c.color }}>{c.icon}</span>
      {method}
    </span>
  );
}

// ─── New Voucher Modal ────────────────────────────────────────────────────────

interface ModalProps {
  nextId: { receipt: string; payment: string };
  onClose: () => void;
  onSave: (v: Voucher) => void;
}

function NewVoucherModal({ nextId, onClose, onSave }: ModalProps) {
  const [type,        setType]        = useState<VoucherType>("receipt");
  const [date,        setDate]        = useState("Mar 28, 2026");
  const [beneficiary, setBeneficiary] = useState("");
  const [description, setDescription] = useState("");
  const [amount,      setAmount]      = useState("");
  const [method,      setMethod]      = useState<PayMethod>("Bank Transfer");
  const [accountCode, setAccountCode] = useState("1101");
  const [ref,         setRef]         = useState("");
  const [notes,       setNotes]       = useState("");

  const selectedAccount = ACCOUNTS.find(a => a.code === accountCode);
  const id = type === "receipt" ? nextId.receipt : nextId.payment;

  const handleSave = (asDraft: boolean) => {
    const voucher: Voucher = {
      id,
      type,
      date,
      beneficiary: beneficiary || "Unknown",
      description: description || "—",
      amount: parseFloat(amount) || 0,
      method,
      account: selectedAccount?.name ?? "",
      accountCode,
      status: asDraft ? "draft" : "approved",
      ref: ref || undefined,
    };
    onSave(voucher);
  };

  const inpStyle: React.CSSProperties = {
    width: "100%", padding: "9px 12px", borderRadius: 8,
    border: "1.5px solid #E2E8F0", fontSize: 12.5, color: "#334155",
    outline: "none", fontFamily: "'Inter', sans-serif", boxSizing: "border-box",
  };
  const labelStyle: React.CSSProperties = {
    color: "#374151", fontSize: 12, fontWeight: 600, display: "block", marginBottom: 6,
  };

  return (
    <div
      onClick={e => { if (e.target === e.currentTarget) onClose(); }}
      style={{ position: "fixed", inset: 0, backgroundColor: "rgba(15,23,42,0.6)", backdropFilter: "blur(4px)", zIndex: 100, display: "flex", alignItems: "center", justifyContent: "center", fontFamily: "'Inter', sans-serif" }}
    >
      <div style={{ width: 680, maxHeight: "90vh", backgroundColor: "#FFFFFF", borderRadius: 18, boxShadow: "0 32px 80px rgba(0,0,0,0.28)", display: "flex", flexDirection: "column", overflow: "hidden" }}>

        {/* Header */}
        <div style={{ padding: "20px 24px 16px", borderBottom: "1px solid #F1F5F9", display: "flex", alignItems: "center", justifyContent: "space-between", flexShrink: 0 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
            <div style={{ width: 40, height: 40, borderRadius: 10, background: type === "receipt" ? "linear-gradient(135deg, #10B981, #059669)" : "linear-gradient(135deg, #EF4444, #DC2626)", display: "flex", alignItems: "center", justifyContent: "center" }}>
              {type === "receipt" ? <ArrowDownLeft size={18} color="white" /> : <ArrowUpRight size={18} color="white" />}
            </div>
            <div>
              <div style={{ color: "#0F172A", fontSize: 15, fontWeight: 800 }}>New Voucher</div>
              <div style={{ color: "#94A3B8", fontSize: 11, marginTop: 1 }}>Voucher #{id}</div>
            </div>
          </div>
          <button onClick={onClose} style={{ width: 32, height: 32, borderRadius: 8, border: "none", backgroundColor: "#F1F5F9", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center" }}>
            <X size={15} style={{ color: "#64748B" }} />
          </button>
        </div>

        {/* Body */}
        <div style={{ flex: 1, overflowY: "auto", padding: "20px 24px" }}>

          {/* Voucher Type Toggle */}
          <div style={{ marginBottom: 20 }}>
            <label style={labelStyle}>Voucher Type <span style={{ color: "#EF4444" }}>*</span></label>
            <div style={{ display: "flex", gap: 0, borderRadius: 10, border: "1.5px solid #E2E8F0", overflow: "hidden" }}>
              {(["receipt", "payment"] as VoucherType[]).map(t => (
                <button key={t} onClick={() => setType(t)}
                  style={{
                    flex: 1, padding: "11px 0", border: "none", cursor: "pointer", fontFamily: "'Inter', sans-serif",
                    fontSize: 13, fontWeight: 700, display: "flex", alignItems: "center", justifyContent: "center", gap: 8,
                    backgroundColor: type === t ? (t === "receipt" ? "#ECFDF5" : "#FEF2F2") : "#FFFFFF",
                    color: type === t ? (t === "receipt" ? "#065F46" : "#991B1B") : "#64748B",
                    transition: "all 0.15s",
                  }}>
                  {t === "receipt" ? <ArrowDownLeft size={14} /> : <ArrowUpRight size={14} />}
                  {t === "receipt" ? "Receipt Voucher (قبض)" : "Payment Voucher (صرف)"}
                </button>
              ))}
            </div>
          </div>

          {/* Row 1: Date + Ref */}
          <div style={{ display: "flex", gap: 14, marginBottom: 14 }}>
            <div style={{ flex: 1 }}>
              <label style={labelStyle}>Date <span style={{ color: "#EF4444" }}>*</span></label>
              <input value={date} onChange={e => setDate(e.target.value)} style={inpStyle} />
            </div>
            <div style={{ flex: 1 }}>
              <label style={labelStyle}>Reference No.</label>
              <input placeholder="e.g. INV-2026-001" value={ref} onChange={e => setRef(e.target.value)} style={inpStyle} />
            </div>
          </div>

          {/* Row 2: Beneficiary */}
          <div style={{ marginBottom: 14 }}>
            <label style={labelStyle}>{type === "receipt" ? "Received From (المستفيد)" : "Paid To (المدفوع له)"} <span style={{ color: "#EF4444" }}>*</span></label>
            <select value={beneficiary} onChange={e => setBeneficiary(e.target.value)}
              style={{ ...inpStyle, cursor: "pointer", color: beneficiary ? "#334155" : "#94A3B8" }}>
              <option value="">Select party…</option>
              {BENEFICIARIES.map(b => <option key={b} value={b}>{b}</option>)}
            </select>
          </div>

          {/* Row 3: Description */}
          <div style={{ marginBottom: 14 }}>
            <label style={labelStyle}>Description / البيان <span style={{ color: "#EF4444" }}>*</span></label>
            <input placeholder="Enter voucher description…" value={description} onChange={e => setDescription(e.target.value)} style={inpStyle} />
          </div>

          {/* Row 4: Amount + Method */}
          <div style={{ display: "flex", gap: 14, marginBottom: 14 }}>
            <div style={{ flex: 1 }}>
              <label style={labelStyle}>Amount (SAR) <span style={{ color: "#EF4444" }}>*</span></label>
              <div style={{ position: "relative" }}>
                <div style={{ position: "absolute", left: 12, top: "50%", transform: "translateY(-50%)", color: "#94A3B8", fontSize: 13, fontWeight: 700 }}>$</div>
                <input type="number" min={0} placeholder="0.00" value={amount} onChange={e => setAmount(e.target.value)}
                  style={{ ...inpStyle, paddingLeft: 28 }} />
              </div>
            </div>
            <div style={{ flex: 1 }}>
              <label style={labelStyle}>Payment Method</label>
              <select value={method} onChange={e => setMethod(e.target.value as PayMethod)}
                style={{ ...inpStyle, cursor: "pointer" }}>
                {(["Cash", "Bank Transfer", "Cheque", "Card"] as PayMethod[]).map(m => (
                  <option key={m} value={m}>{m}</option>
                ))}
              </select>
            </div>
          </div>

          {/* Row 5: Account */}
          <div style={{ marginBottom: 14 }}>
            <label style={labelStyle}>Account (الحساب) <span style={{ color: "#EF4444" }}>*</span></label>
            <select value={accountCode} onChange={e => setAccountCode(e.target.value)}
              style={{ ...inpStyle, cursor: "pointer" }}>
              {ACCOUNTS.map(a => <option key={a.code} value={a.code}>{a.code} — {a.name}</option>)}
            </select>
          </div>

          {/* Row 6: Notes */}
          <div style={{ marginBottom: 4 }}>
            <label style={labelStyle}>Notes / ملاحظات</label>
            <textarea placeholder="Additional notes…" value={notes} onChange={e => setNotes(e.target.value)} rows={2}
              style={{ ...inpStyle, resize: "none" }} />
          </div>

          {/* Amount Preview */}
          {parseFloat(amount) > 0 && (
            <div style={{ marginTop: 14, padding: "12px 16px", borderRadius: 10, backgroundColor: type === "receipt" ? "#ECFDF5" : "#FEF2F2", border: `1px solid ${type === "receipt" ? "#A7F3D0" : "#FECACA"}`, display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <span style={{ fontSize: 12.5, color: type === "receipt" ? "#065F46" : "#991B1B", fontWeight: 600 }}>
                {type === "receipt" ? "▼ Amount to Receive" : "▲ Amount to Pay"}
              </span>
              <span style={{ fontSize: 18, fontWeight: 900, color: type === "receipt" ? "#059669" : "#DC2626" }}>
                ${parseFloat(amount).toLocaleString("en-US", { minimumFractionDigits: 2 })}
              </span>
            </div>
          )}
        </div>

        {/* Footer */}
        <div style={{ padding: "14px 24px", borderTop: "1px solid #F1F5F9", display: "flex", gap: 10, justifyContent: "flex-end", flexShrink: 0 }}>
          <button onClick={onClose} style={{ padding: "9px 20px", borderRadius: 9, border: "1.5px solid #E2E8F0", backgroundColor: "#FFFFFF", color: "#64748B", fontSize: 13, fontWeight: 600, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>Cancel</button>
          <button onClick={() => handleSave(true)} style={{ padding: "9px 18px", borderRadius: 9, border: "1.5px solid #1E3A5F", backgroundColor: "#FFFFFF", color: "#1E3A5F", fontSize: 13, fontWeight: 600, cursor: "pointer", fontFamily: "'Inter', sans-serif", display: "flex", alignItems: "center", gap: 6 }}>
            <FileText size={13} /> Save Draft
          </button>
          <button onClick={() => handleSave(false)} style={{ padding: "9px 22px", borderRadius: 9, border: "none", backgroundColor: "#1E3A5F", color: "#FFFFFF", fontSize: 13, fontWeight: 700, cursor: "pointer", fontFamily: "'Inter', sans-serif", display: "flex", alignItems: "center", gap: 7 }}>
            <Stamp size={13} /> Approve & Post
          </button>
        </div>
      </div>
    </div>
  );
}

// ─── Voucher Detail Panel ─────────────────────────────────────────────────────

function VoucherDetail({ voucher, onApprove, onCancel }: {
  voucher: Voucher;
  onApprove: (id: string) => void;
  onCancel:  (id: string) => void;
}) {
  const isReceipt = voucher.type === "receipt";
  const mc = METHOD_CFG[voucher.method];
  const sc = STATUS_CFG[voucher.status];

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 12, fontFamily: "'Inter', sans-serif" }}>

      {/* Header card */}
      <div style={{ background: isReceipt ? "linear-gradient(135deg, #1E3A5F, #065F46)" : "linear-gradient(135deg, #1E3A5F, #7F1D1D)", borderRadius: 12, padding: 18, boxShadow: "0 4px 16px rgba(30,58,95,0.25)" }}>
        <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", marginBottom: 14 }}>
          <div>
            <div style={{ color: "rgba(255,255,255,0.55)", fontSize: 10, fontWeight: 600, letterSpacing: "0.6px", textTransform: "uppercase", marginBottom: 4 }}>
              {isReceipt ? "Receipt Voucher — سند قبض" : "Payment Voucher — سند صرف"}
            </div>
            <div style={{ color: "#FFFFFF", fontSize: 15, fontWeight: 800, fontFamily: "monospace", letterSpacing: "0.5px" }}>{voucher.id}</div>
          </div>
          <span style={{ backgroundColor: sc.bg, color: sc.color, fontSize: 10, fontWeight: 700, padding: "3px 9px", borderRadius: 12, border: `1px solid ${sc.border}`, display: "flex", alignItems: "center", gap: 4 }}>
            <span style={{ width: 5, height: 5, borderRadius: "50%", backgroundColor: sc.dot, display: "inline-block" }} />
            {sc.label}
          </span>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 8 }}>
          {[
            { label: "Amount",      value: `$${voucher.amount.toLocaleString("en-US", { minimumFractionDigits: 2 })}`, color: "#FFFFFF", big: true },
            { label: "Date",        value: voucher.date, color: "rgba(255,255,255,0.85)", big: false },
            { label: "Account",     value: `${voucher.accountCode} — ${voucher.account}`, color: "rgba(255,255,255,0.85)", big: false },
            { label: "Method",      value: voucher.method, color: "rgba(255,255,255,0.85)", big: false },
          ].map(m => (
            <div key={m.label} style={{ backgroundColor: "rgba(255,255,255,0.08)", borderRadius: 8, padding: "8px 10px" }}>
              <div style={{ color: "rgba(255,255,255,0.4)", fontSize: 9.5, marginBottom: 3, textTransform: "uppercase", letterSpacing: "0.4px" }}>{m.label}</div>
              <div style={{ color: m.color, fontSize: m.big ? 17 : 11.5, fontWeight: m.big ? 900 : 600 }}>{m.value}</div>
            </div>
          ))}
        </div>
      </div>

      {/* Beneficiary */}
      <div style={{ backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: 12, padding: 14, boxShadow: "0 1px 4px rgba(0,0,0,0.05)" }}>
        <div style={{ color: "#64748B", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.6px", marginBottom: 10 }}>Voucher Details</div>
        <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
          {[
            { label: isReceipt ? "Received From" : "Paid To", value: voucher.beneficiary },
            { label: "Description / البيان", value: voucher.description },
            { label: "Reference", value: voucher.ref ?? "—" },
            { label: "Approved By", value: voucher.approvedBy ?? "Pending" },
          ].map(row => (
            <div key={row.label} style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", padding: "6px 0", borderBottom: "1px solid #F8FAFC" }}>
              <span style={{ color: "#94A3B8", fontSize: 11.5 }}>{row.label}</span>
              <span style={{ color: "#0F172A", fontSize: 12, fontWeight: 600, textAlign: "right", maxWidth: "55%" }}>{row.value}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Accounting Entry Preview */}
      <div style={{ backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: 12, padding: 14, boxShadow: "0 1px 4px rgba(0,0,0,0.05)" }}>
        <div style={{ color: "#64748B", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.6px", marginBottom: 10 }}>Accounting Entry Preview</div>
        <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 11 }}>
          <thead>
            <tr style={{ backgroundColor: "#F8FAFC" }}>
              {["Account", "Debit", "Credit"].map(h => (
                <th key={h} style={{ padding: "6px 8px", textAlign: h === "Account" ? "left" : "right", color: "#64748B", fontSize: 9.5, fontWeight: 700, textTransform: "uppercase", borderBottom: "1px solid #E2E8F0" }}>{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            <tr style={{ borderBottom: "1px solid #F1F5F9" }}>
              <td style={{ padding: "8px 8px" }}>
                <div style={{ color: "#1E3A5F", fontSize: 11.5, fontWeight: 700 }}>{voucher.accountCode}</div>
                <div style={{ color: "#64748B", fontSize: 10 }}>{voucher.account}</div>
              </td>
              <td style={{ padding: "8px 8px", textAlign: "right", color: "#0F172A", fontWeight: 700, fontSize: 12 }}>
                {isReceipt ? `$${voucher.amount.toLocaleString("en-US", { minimumFractionDigits: 2 })}` : "—"}
              </td>
              <td style={{ padding: "8px 8px", textAlign: "right", color: "#0F172A", fontWeight: 700, fontSize: 12 }}>
                {!isReceipt ? `$${voucher.amount.toLocaleString("en-US", { minimumFractionDigits: 2 })}` : "—"}
              </td>
            </tr>
          </tbody>
          <tbody>
            <tr>
              <td style={{ padding: "8px 8px" }}>
                <div style={{ color: "#1E3A5F", fontSize: 11.5, fontWeight: 700 }}>{isReceipt ? "1201" : "2101"}</div>
                <div style={{ color: "#64748B", fontSize: 10 }}>{isReceipt ? "Accounts Receivable" : "Accounts Payable"}</div>
              </td>
              <td style={{ padding: "8px 8px", textAlign: "right", color: "#0F172A", fontWeight: 700, fontSize: 12 }}>
                {!isReceipt ? `$${voucher.amount.toLocaleString("en-US", { minimumFractionDigits: 2 })}` : "—"}
              </td>
              <td style={{ padding: "8px 8px", textAlign: "right", color: "#0F172A", fontWeight: 700, fontSize: 12 }}>
                {isReceipt ? `$${voucher.amount.toLocaleString("en-US", { minimumFractionDigits: 2 })}` : "—"}
              </td>
            </tr>
          </tbody>
          <tbody>
            <tr style={{ backgroundColor: "#F8FAFC" }}>
              <td style={{ padding: "6px 8px", color: "#1E3A5F", fontSize: 11, fontWeight: 800 }}>Total</td>
              <td style={{ padding: "6px 8px", textAlign: "right", color: "#1E3A5F", fontSize: 12, fontWeight: 900 }}>${voucher.amount.toLocaleString("en-US", { minimumFractionDigits: 2 })}</td>
              <td style={{ padding: "6px 8px", textAlign: "right", color: "#1E3A5F", fontSize: 12, fontWeight: 900 }}>${voucher.amount.toLocaleString("en-US", { minimumFractionDigits: 2 })}</td>
            </tr>
          </tbody>
        </table>
        <div style={{ marginTop: 8, display: "flex", alignItems: "center", gap: 5, color: "#10B981", fontSize: 10.5, fontWeight: 600 }}>
          <CheckCircle size={11} /> Balanced — المدين يساوي الدائن
        </div>
      </div>

      {/* Actions */}
      {voucher.status === "draft" && (
        <div style={{ display: "flex", gap: 8 }}>
          <button onClick={() => onApprove(voucher.id)}
            style={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center", gap: 6, padding: "9px 0", borderRadius: 9, border: "none", backgroundColor: "#1E3A5F", color: "#FFFFFF", fontSize: 12, fontWeight: 700, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <Stamp size={13} /> Approve & Post
          </button>
          <button onClick={() => onCancel(voucher.id)}
            style={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center", gap: 6, padding: "9px 0", borderRadius: 9, border: "1.5px solid #E2E8F0", backgroundColor: "#FFFFFF", color: "#64748B", fontSize: 12, fontWeight: 600, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <XCircle size={13} /> Cancel
          </button>
        </div>
      )}
      {voucher.status === "approved" && (
        <button style={{ width: "100%", display: "flex", alignItems: "center", justifyContent: "center", gap: 6, padding: "9px 0", borderRadius: 9, border: "1px solid #E2E8F0", backgroundColor: "#F8FAFC", color: "#64748B", fontSize: 12, fontWeight: 600, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
          <Printer size={13} /> Print Voucher
        </button>
      )}
    </div>
  );
}

// ─── Main Component ───────────────────────────────────────────────────────────

export function VouchersTab() {
  const [vouchers,      setVouchers]      = useState<Voucher[]>(SEED);
  const [activeFilter,  setActiveFilter]  = useState("All");
  const [search,        setSearch]        = useState("");
  const [statusFilter,  setStatusFilter]  = useState("All");
  const [selectedId,    setSelectedId]    = useState<string>(SEED[0].id);
  const [showModal,     setShowModal]     = useState(false);

  const selected = vouchers.find(v => v.id === selectedId) ?? vouchers[0];

  const filtered = useMemo(() => {
    return vouchers.filter(v => {
      const matchType   = activeFilter === "All" || v.type === activeFilter;
      const matchStatus = statusFilter === "All" || v.status === statusFilter;
      const q           = search.toLowerCase();
      const matchSearch = !q || v.id.toLowerCase().includes(q) || v.beneficiary.toLowerCase().includes(q) || v.description.toLowerCase().includes(q);
      return matchType && matchStatus && matchSearch;
    });
  }, [vouchers, activeFilter, search, statusFilter]);

  // KPIs
  const totalAmount    = vouchers.reduce((s, v) => s + v.amount, 0);
  const receiptTotal   = vouchers.filter(v => v.type === "receipt").reduce((s, v) => s + v.amount, 0);
  const paymentTotal   = vouchers.filter(v => v.type === "payment").reduce((s, v) => s + v.amount, 0);
  const draftCount     = vouchers.filter(v => v.status === "draft").length;

  const kpis = [
    { title: "Total Vouchers",    value: `${vouchers.length}`,                                       sub: "All types",           color: "#3B82F6", bg: "#EFF6FF", icon: <FileText size={18} /> },
    { title: "Receipt Vouchers",  value: `$${(receiptTotal / 1000).toFixed(1)}k`,                    sub: `${vouchers.filter(v => v.type === "receipt").length} vouchers`,  color: "#10B981", bg: "#ECFDF5", icon: <ArrowDownLeft size={18} /> },
    { title: "Payment Vouchers",  value: `$${(paymentTotal / 1000).toFixed(1)}k`,                    sub: `${vouchers.filter(v => v.type === "payment").length} vouchers`,  color: "#EF4444", bg: "#FEF2F2", icon: <ArrowUpRight size={18} /> },
    { title: "Pending Approval",  value: `${draftCount}`,                                             sub: "Awaiting review",     color: "#F59E0B", bg: "#FFFBEB", icon: <Clock size={18} /> },
  ];

  const nextReceiptId = `RV-2026-0${(vouchers.filter(v => v.type === "receipt").length + 1).toString().padStart(2, "0")}`;
  const nextPaymentId = `PV-2026-0${(vouchers.filter(v => v.type === "payment").length + 1).toString().padStart(2, "0")}`;

  const handleSave = (v: Voucher) => { setVouchers(prev => [v, ...prev]); setSelectedId(v.id); setShowModal(false); };
  const handleApprove = (id: string) => setVouchers(prev => prev.map(v => v.id === id ? { ...v, status: "approved", approvedBy: "Ahmed Hassan" } : v));
  const handleCancel  = (id: string) => setVouchers(prev => prev.map(v => v.id === id ? { ...v, status: "cancelled" } : v));

  const tabCount = (key: string) => key === "All" ? vouchers.length : vouchers.filter(v => v.type === key).length;

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 16, fontFamily: "'Inter', sans-serif" }}>

      {/* ── Page Header ── */}
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div>
          <h2 style={{ margin: 0, color: "#0F172A", fontSize: 16, fontWeight: 800 }}>Vouchers — السندات</h2>
          <p style={{ margin: "2px 0 0", color: "#94A3B8", fontSize: 12 }}>Manage receipt & payment vouchers with full accounting entry preview · FY 2025–26</p>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <button style={{ display: "flex", alignItems: "center", gap: 6, padding: "8px 14px", borderRadius: 9, border: "1px solid #E2E8F0", backgroundColor: "#FFFFFF", color: "#64748B", fontSize: 12, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <Download size={13} /> Export
          </button>
          <button onClick={() => setShowModal(true)}
            style={{ display: "flex", alignItems: "center", gap: 6, padding: "8px 16px", borderRadius: 9, border: "none", backgroundColor: "#1E3A5F", color: "#FFFFFF", fontSize: 12, fontWeight: 700, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <Plus size={14} /> New Voucher
          </button>
        </div>
      </div>

      {/* ── KPI Cards ── */}
      <div style={{ display: "flex", gap: 14 }}>
        {kpis.map(card => (
          <div key={card.title} style={{ flex: 1, backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: 12, padding: "15px 18px", boxShadow: "0 1px 4px rgba(0,0,0,0.05)", display: "flex", alignItems: "center", gap: 12, position: "relative", overflow: "hidden" }}>
            <div style={{ position: "absolute", top: 0, left: 0, right: 0, height: 3, backgroundColor: card.color, borderRadius: "12px 12px 0 0" }} />
            <div style={{ width: 40, height: 40, borderRadius: 10, backgroundColor: card.bg, display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
              <span style={{ color: card.color }}>{card.icon}</span>
            </div>
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ color: "#64748B", fontSize: 11, fontWeight: 500, marginBottom: 2 }}>{card.title}</div>
              <div style={{ color: "#0F172A", fontSize: 20, fontWeight: 800, letterSpacing: "-0.4px", lineHeight: 1 }}>{card.value}</div>
              <div style={{ color: "#94A3B8", fontSize: 10, marginTop: 3 }}>{card.sub}</div>
            </div>
          </div>
        ))}
      </div>

      {/* ── Main Area: Table + Detail ── */}
      <div style={{ display: "flex", gap: 16, alignItems: "flex-start" }}>

        {/* ── Left: Table ── */}
        <div style={{ flex: 1, backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: 12, boxShadow: "0 1px 4px rgba(0,0,0,0.05)", overflow: "hidden", minWidth: 0 }}>

          {/* Toolbar */}
          <div style={{ padding: "12px 18px", borderBottom: "1px solid #F1F5F9", display: "flex", alignItems: "center", justifyContent: "space-between", gap: 10 }}>
            {/* Type tabs */}
            <div style={{ display: "flex", gap: 2, backgroundColor: "#F5F7FA", padding: 3, borderRadius: 10, flexShrink: 0 }}>
              {FILTER_TABS.map(tab => (
                <button key={tab.key} onClick={() => setActiveFilter(tab.key)}
                  style={{ padding: "4px 10px", borderRadius: 7, border: "none", backgroundColor: activeFilter === tab.key ? "#FFFFFF" : "transparent", color: activeFilter === tab.key ? "#1E3A5F" : "#64748B", fontSize: 11, fontWeight: activeFilter === tab.key ? 700 : 400, cursor: "pointer", boxShadow: activeFilter === tab.key ? "0 1px 3px rgba(0,0,0,0.1)" : "none", fontFamily: "'Inter', sans-serif", whiteSpace: "nowrap", display: "flex", alignItems: "center", gap: 5 }}>
                  {tab.label}
                  <span style={{ backgroundColor: activeFilter === tab.key ? "#EFF6FF" : "#E2E8F0", color: activeFilter === tab.key ? "#1E3A5F" : "#94A3B8", fontSize: 9.5, fontWeight: 700, padding: "1px 5px", borderRadius: 8 }}>
                    {tabCount(tab.key)}
                  </span>
                </button>
              ))}
            </div>
            {/* Search + Status */}
            <div style={{ display: "flex", gap: 8 }}>
              <div style={{ display: "flex", alignItems: "center", gap: 7, padding: "6px 12px", borderRadius: 8, border: "1px solid #E2E8F0", backgroundColor: "#F8FAFC", width: 180 }}>
                <Search size={12} style={{ color: "#94A3B8", flexShrink: 0 }} />
                <input type="text" placeholder="Search vouchers…" value={search} onChange={e => setSearch(e.target.value)}
                  style={{ flex: 1, border: "none", outline: "none", background: "transparent", color: "#334155", fontSize: 12, fontFamily: "'Inter', sans-serif" }} />
              </div>
              <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}
                style={{ padding: "6px 10px", borderRadius: 8, border: "1px solid #E2E8F0", backgroundColor: "#F8FAFC", color: "#64748B", fontSize: 11.5, cursor: "pointer", fontFamily: "'Inter', sans-serif", outline: "none" }}>
                <option value="All">All Status</option>
                <option value="draft">Draft</option>
                <option value="approved">Approved</option>
                <option value="cancelled">Cancelled</option>
              </select>
            </div>
          </div>

          {/* Table */}
          <div style={{ overflowY: "auto", maxHeight: 420 }}>
            <table style={{ width: "100%", borderCollapse: "collapse" }}>
              <thead style={{ position: "sticky", top: 0, zIndex: 2 }}>
                <tr style={{ backgroundColor: "#F8FAFC" }}>
                  {["Voucher #", "Type", "Date", "Beneficiary / Party", "Description", "Method", "Amount", "Status"].map(h => (
                    <th key={h} style={{ padding: "8px 12px", textAlign: h === "Amount" ? "right" : "left", color: "#64748B", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.4px", borderBottom: "1px solid #E2E8F0", backgroundColor: "#F8FAFC", whiteSpace: "nowrap" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {filtered.map((v, idx) => {
                  const isSelected = selectedId === v.id;
                  return (
                    <tr key={v.id}
                      onClick={() => setSelectedId(v.id)}
                      style={{ backgroundColor: isSelected ? "#EFF6FF" : idx % 2 === 0 ? "#FFFFFF" : "#FAFBFC", borderBottom: "1px solid #F1F5F9", cursor: "pointer", borderLeft: isSelected ? "3px solid #3B82F6" : "3px solid transparent" }}
                      onMouseEnter={e => { if (!isSelected) (e.currentTarget as HTMLTableRowElement).style.backgroundColor = "#F8FAFC"; }}
                      onMouseLeave={e => { if (!isSelected) (e.currentTarget as HTMLTableRowElement).style.backgroundColor = idx % 2 === 0 ? "#FFFFFF" : "#FAFBFC"; }}>
                      <td style={{ padding: "10px 12px" }}>
                        <span style={{ color: "#3B82F6", fontSize: 11, fontWeight: 700, fontFamily: "monospace" }}>{v.id}</span>
                      </td>
                      <td style={{ padding: "10px 12px" }}><TypeBadge type={v.type} /></td>
                      <td style={{ padding: "10px 12px", color: "#64748B", fontSize: 11.5, whiteSpace: "nowrap" }}>{v.date}</td>
                      <td style={{ padding: "10px 12px" }}>
                        <div style={{ color: "#0F172A", fontSize: 12.5, fontWeight: 600, whiteSpace: "nowrap" }}>{v.beneficiary}</div>
                        {v.ref && <div style={{ color: "#94A3B8", fontSize: 10, marginTop: 1 }}>Ref: {v.ref}</div>}
                      </td>
                      <td style={{ padding: "10px 12px", color: "#475569", fontSize: 11.5, maxWidth: 200 }}>
                        <div style={{ overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{v.description}</div>
                      </td>
                      <td style={{ padding: "10px 12px" }}><MethodBadge method={v.method} /></td>
                      <td style={{ padding: "10px 12px", textAlign: "right", whiteSpace: "nowrap" }}>
                        <span style={{ color: v.type === "receipt" ? "#059669" : "#DC2626", fontWeight: 800, fontSize: 13 }}>
                          {v.type === "receipt" ? "+" : "−"}${v.amount.toLocaleString("en-US", { minimumFractionDigits: 2 })}
                        </span>
                      </td>
                      <td style={{ padding: "10px 12px" }}><StatusBadge status={v.status} /></td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
            {filtered.length === 0 && (
              <div style={{ padding: "40px 0", textAlign: "center", color: "#94A3B8", fontSize: 13 }}>No vouchers found</div>
            )}
          </div>

          {/* Footer */}
          <div style={{ padding: "10px 18px", borderTop: "1px solid #F1F5F9", display: "flex", justifyContent: "space-between", alignItems: "center", backgroundColor: "#FAFBFC" }}>
            <span style={{ color: "#94A3B8", fontSize: 11 }}>{filtered.length} voucher{filtered.length !== 1 ? "s" : ""} shown</span>
            <div style={{ display: "flex", gap: 16 }}>
              <span style={{ fontSize: 11.5, color: "#059669", fontWeight: 700 }}>
                Receipts: +${filtered.filter(v => v.type === "receipt" && v.status === "approved").reduce((s, v) => s + v.amount, 0).toLocaleString()}
              </span>
              <span style={{ fontSize: 11.5, color: "#DC2626", fontWeight: 700 }}>
                Payments: −${filtered.filter(v => v.type === "payment" && v.status === "approved").reduce((s, v) => s + v.amount, 0).toLocaleString()}
              </span>
            </div>
          </div>
        </div>

        {/* ── Right: Detail Panel ── */}
        <div style={{ width: 280, flexShrink: 0, overflowY: "auto", maxHeight: 540 }}>
          {selected && (
            <VoucherDetail
              voucher={selected}
              onApprove={handleApprove}
              onCancel={handleCancel}
            />
          )}
        </div>
      </div>

      {showModal && (
        <NewVoucherModal
          nextId={{ receipt: nextReceiptId, payment: nextPaymentId }}
          onClose={() => setShowModal(false)}
          onSave={handleSave}
        />
      )}
    </div>
  );
}
