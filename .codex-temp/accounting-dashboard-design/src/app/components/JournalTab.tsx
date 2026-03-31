import React, { useState, useMemo } from "react";
import {
  Search, Plus, Download, X, CheckCircle, Clock, AlertTriangle,
  ArrowUpRight, RotateCcw, BookOpen, FileText, Filter,
  ChevronRight, Eye, Stamp, Layers, Printer,
} from "lucide-react";

// ─── Types ───────────────────────────────────────────────────────────────────

type EntryStatus = "draft" | "posted" | "reversed";

interface JournalLine {
  id: string;
  accountCode: string;
  accountName: string;
  description: string;
  debit: number;
  credit: number;
}

interface JournalEntry {
  id: string;
  date: string;
  reference: string;
  description: string;
  lines: JournalLine[];
  totalDebit: number;
  totalCredit: number;
  status: EntryStatus;
  createdBy: string;
  postedDate?: string;
  category: string;
}

// ─── Chart of Accounts (for modal) ───────────────────────────────────────────

const COA = [
  { code: "1101", name: "Cash & Bank — Main",       type: "Asset"    },
  { code: "1102", name: "Petty Cash",                type: "Asset"    },
  { code: "1201", name: "Accounts Receivable",       type: "Asset"    },
  { code: "1301", name: "Inventory — Merchandise",   type: "Asset"    },
  { code: "1401", name: "Prepaid Expenses",           type: "Asset"    },
  { code: "1501", name: "Fixed Assets — Equipment",  type: "Asset"    },
  { code: "2101", name: "Accounts Payable",          type: "Liability" },
  { code: "2201", name: "Accrued Liabilities",       type: "Liability" },
  { code: "2301", name: "VAT Payable",               type: "Liability" },
  { code: "3101", name: "Share Capital",             type: "Equity"   },
  { code: "3201", name: "Retained Earnings",         type: "Equity"   },
  { code: "4101", name: "Sales Revenue",             type: "Revenue"  },
  { code: "4201", name: "Service Revenue",           type: "Revenue"  },
  { code: "5101", name: "Cost of Goods Sold",        type: "Expense"  },
  { code: "6101", name: "Office Expense",            type: "Expense"  },
  { code: "6102", name: "Rent Expense",              type: "Expense"  },
  { code: "6201", name: "Utilities Expense",         type: "Expense"  },
  { code: "6301", name: "Transport Expense",         type: "Expense"  },
  { code: "6401", name: "Salaries Expense",          type: "Expense"  },
  { code: "6501", name: "Depreciation Expense",      type: "Expense"  },
];

// ─── Seed Journal Entries ─────────────────────────────────────────────────────

const SEED_ENTRIES: JournalEntry[] = [
  {
    id: "JE-2026-001", date: "Mar 01, 2026", reference: "RV-2026-001", description: "Receipt from TechCorp Solutions — INV-2026-001",
    category: "Accounts Receivable", status: "posted", createdBy: "Ahmed Hassan", postedDate: "Mar 01, 2026",
    totalDebit: 3200, totalCredit: 3200,
    lines: [
      { id: "l1", accountCode: "1101", accountName: "Cash & Bank — Main",   description: "Bank receipt — TechCorp",        debit: 3200, credit: 0    },
      { id: "l2", accountCode: "1201", accountName: "Accounts Receivable",  description: "Settle AR — INV-2026-001",       debit: 0,    credit: 3200 },
    ],
  },
  {
    id: "JE-2026-002", date: "Mar 02, 2026", reference: "PV-2026-001", description: "Payment to NextGen Supplies — PO-2026-055 (partial)",
    category: "Accounts Payable", status: "posted", createdBy: "Ahmed Hassan", postedDate: "Mar 02, 2026",
    totalDebit: 6500, totalCredit: 6500,
    lines: [
      { id: "l1", accountCode: "2101", accountName: "Accounts Payable",     description: "Settle AP — NextGen / PO-055",   debit: 6500, credit: 0    },
      { id: "l2", accountCode: "1101", accountName: "Cash & Bank — Main",   description: "Bank payment — NextGen",         debit: 0,    credit: 6500 },
    ],
  },
  {
    id: "JE-2026-003", date: "Mar 04, 2026", reference: "PV-2026-002", description: "Utility bills payment — February 2026",
    category: "Expenses", status: "posted", createdBy: "Sara Al-Rashid", postedDate: "Mar 04, 2026",
    totalDebit: 1240, totalCredit: 1240,
    lines: [
      { id: "l1", accountCode: "6201", accountName: "Utilities Expense",    description: "Electricity & water — Feb 2026", debit: 1240, credit: 0    },
      { id: "l2", accountCode: "1101", accountName: "Cash & Bank — Main",   description: "Cheque payment — Utility Co.",   debit: 0,    credit: 1240 },
    ],
  },
  {
    id: "JE-2026-004", date: "Mar 05, 2026", reference: "RV-2026-002", description: "Cheque collection — Al-Farhan Trading INV-2026-002",
    category: "Accounts Receivable", status: "posted", createdBy: "Ahmed Hassan", postedDate: "Mar 05, 2026",
    totalDebit: 7800, totalCredit: 7800,
    lines: [
      { id: "l1", accountCode: "1101", accountName: "Cash & Bank — Main",   description: "Cheque deposit — Al-Farhan",     debit: 7800, credit: 0    },
      { id: "l2", accountCode: "1201", accountName: "Accounts Receivable",  description: "Settle AR — INV-2026-002",       debit: 0,    credit: 7800 },
    ],
  },
  {
    id: "JE-2026-005", date: "Mar 10, 2026", reference: "PV-2026-003", description: "Advance payment to Global Imports LLC — PO-2026-057",
    category: "Accounts Payable", status: "posted", createdBy: "Sara Al-Rashid", postedDate: "Mar 10, 2026",
    totalDebit: 14200, totalCredit: 14200,
    lines: [
      { id: "l1", accountCode: "2101", accountName: "Accounts Payable",     description: "AP reduction — Global Imports",  debit: 14200, credit: 0    },
      { id: "l2", accountCode: "1101", accountName: "Cash & Bank — Main",   description: "Bank transfer — Global Imports", debit: 0,     credit: 14200 },
    ],
  },
  {
    id: "JE-2026-006", date: "Mar 12, 2026", reference: "PV-2026-005", description: "March 2026 office rent payment",
    category: "Expenses", status: "posted", createdBy: "Ahmed Hassan", postedDate: "Mar 12, 2026",
    totalDebit: 8500, totalCredit: 8500,
    lines: [
      { id: "l1", accountCode: "6102", accountName: "Rent Expense",         description: "Office rent — March 2026",       debit: 8500, credit: 0    },
      { id: "l2", accountCode: "1101", accountName: "Cash & Bank — Main",   description: "Bank transfer — Office Rent LLC",debit: 0,    credit: 8500 },
    ],
  },
  {
    id: "JE-2026-007", date: "Mar 15, 2026", reference: "SAL-MAR26", description: "March 2026 salaries — all departments",
    category: "Payroll", status: "posted", createdBy: "Sara Al-Rashid", postedDate: "Mar 15, 2026",
    totalDebit: 42500, totalCredit: 42500,
    lines: [
      { id: "l1", accountCode: "6401", accountName: "Salaries Expense",     description: "Gross salaries — March 2026",    debit: 42500, credit: 0    },
      { id: "l2", accountCode: "1101", accountName: "Cash & Bank — Main",   description: "Payroll transfer — March 2026",  debit: 0,     credit: 42500 },
    ],
  },
  {
    id: "JE-2026-008", date: "Mar 18, 2026", reference: "DEP-Q1-26", description: "Q1 2026 depreciation — equipment & fixtures",
    category: "Depreciation", status: "posted", createdBy: "Ahmed Hassan", postedDate: "Mar 18, 2026",
    totalDebit: 3800, totalCredit: 3800,
    lines: [
      { id: "l1", accountCode: "6501", accountName: "Depreciation Expense", description: "Q1 depreciation charge",         debit: 3800, credit: 0    },
      { id: "l2", accountCode: "1501", accountName: "Fixed Assets — Equipment", description: "Accumulated depreciation",   debit: 0,    credit: 3800 },
    ],
  },
  {
    id: "JE-2026-009", date: "Mar 20, 2026", reference: "INV-COGS-026", description: "Cost of goods sold — batch sale to Sunrise Retail",
    category: "COGS", status: "posted", createdBy: "Ahmed Hassan", postedDate: "Mar 20, 2026",
    totalDebit: 6800, totalCredit: 6800,
    lines: [
      { id: "l1", accountCode: "5101", accountName: "Cost of Goods Sold",   description: "COGS — Sunrise batch",           debit: 6800, credit: 0    },
      { id: "l2", accountCode: "1301", accountName: "Inventory — Merchandise", description: "Inventory reduction",         debit: 0,    credit: 6800 },
    ],
  },
  {
    id: "JE-2026-010", date: "Mar 22, 2026", reference: "ADJ-VAT-MAR26", description: "VAT payable accrual — March 2026 sales tax",
    category: "Tax", status: "draft", createdBy: "Sara Al-Rashid",
    totalDebit: 5840, totalCredit: 5840,
    lines: [
      { id: "l1", accountCode: "4101", accountName: "Sales Revenue",        description: "VAT component — March sales",    debit: 5840, credit: 0    },
      { id: "l2", accountCode: "2301", accountName: "VAT Payable",          description: "VAT payable accrual",            debit: 0,    credit: 5840 },
    ],
  },
  {
    id: "JE-2026-011", date: "Mar 25, 2026", reference: "ACCRUAL-EXP-026", description: "Accrued expenses — end of March closing",
    category: "Accruals", status: "draft", createdBy: "Ahmed Hassan",
    totalDebit: 2300, totalCredit: 2300,
    lines: [
      { id: "l1", accountCode: "6101", accountName: "Office Expense",       description: "Accrued office expense",         debit: 1200, credit: 0    },
      { id: "l2", accountCode: "6301", accountName: "Transport Expense",    description: "Accrued freight charges",        debit: 1100, credit: 0    },
      { id: "l3", accountCode: "2201", accountName: "Accrued Liabilities",  description: "Expense accrual — March",        debit: 0,    credit: 2300 },
    ],
  },
  {
    id: "JE-2026-012", date: "Feb 28, 2026", reference: "REV-JE-2026-008B", description: "Reversal: Duplicate depreciation entry — Feb batch",
    category: "Reversal", status: "reversed", createdBy: "Sara Al-Rashid", postedDate: "Feb 28, 2026",
    totalDebit: 1200, totalCredit: 1200,
    lines: [
      { id: "l1", accountCode: "1501", accountName: "Fixed Assets — Equipment", description: "Reversal — duplicate dep.",  debit: 1200, credit: 0    },
      { id: "l2", accountCode: "6501", accountName: "Depreciation Expense", description: "Reversal — duplicate dep.",      debit: 0,    credit: 1200 },
    ],
  },
];

const CATEGORIES = ["All", "Accounts Receivable", "Accounts Payable", "Expenses", "Payroll", "COGS", "Depreciation", "Tax", "Accruals", "Reversal"];

// ─── Config ───────────────────────────────────────────────────────────────────

const STATUS_CFG: Record<EntryStatus, { label: string; arabicLabel: string; color: string; bg: string; dot: string; border: string }> = {
  draft:    { label: "Draft",    arabicLabel: "مسودة",   color: "#4B5563", bg: "#F9FAFB", dot: "#9CA3AF", border: "#E5E7EB" },
  posted:   { label: "Posted",   arabicLabel: "مُعتمد",  color: "#065F46", bg: "#ECFDF5", dot: "#10B981", border: "#A7F3D0" },
  reversed: { label: "Reversed", arabicLabel: "مُعكوس", color: "#6D28D9", bg: "#F5F3FF", dot: "#8B5CF6", border: "#DDD6FE" },
};

const CAT_COLOR: Record<string, { color: string; bg: string }> = {
  "Accounts Receivable": { color: "#1D4ED8", bg: "#DBEAFE" },
  "Accounts Payable":    { color: "#92400E", bg: "#FDE68A" },
  "Expenses":            { color: "#991B1B", bg: "#FEE2E2" },
  "Payroll":             { color: "#065F46", bg: "#D1FAE5" },
  "COGS":                { color: "#1E3A5F", bg: "#DBEAFE" },
  "Depreciation":        { color: "#4B5563", bg: "#F1F5F9" },
  "Tax":                 { color: "#92400E", bg: "#FEF3C7" },
  "Accruals":            { color: "#6D28D9", bg: "#EDE9FE" },
  "Reversal":            { color: "#9F1239", bg: "#FCE7F3" },
};

// ─── Sub-components ───────────────────────────────────────────────────────────

function StatusBadge({ status }: { status: EntryStatus }) {
  const c = STATUS_CFG[status];
  return (
    <span style={{ display: "inline-flex", alignItems: "center", gap: 4, backgroundColor: c.bg, color: c.color, fontSize: "10.5px", fontWeight: 600, padding: "2px 8px", borderRadius: 20, border: `1px solid ${c.border}`, whiteSpace: "nowrap" }}>
      <span style={{ width: 5, height: 5, borderRadius: "50%", backgroundColor: c.dot, flexShrink: 0 }} />
      {c.label}
    </span>
  );
}

function CatBadge({ category }: { category: string }) {
  const c = CAT_COLOR[category] ?? { color: "#64748B", bg: "#F1F5F9" };
  return (
    <span style={{ display: "inline-flex", alignItems: "center", backgroundColor: c.bg, color: c.color, fontSize: "9.5px", fontWeight: 600, padding: "2px 7px", borderRadius: 8, whiteSpace: "nowrap" }}>
      {category}
    </span>
  );
}

// ─── New Journal Entry Modal ──────────────────────────────────────────────────

interface NewEntryModalProps {
  nextId: string;
  onClose: () => void;
  onSave: (entry: JournalEntry) => void;
}

function NewEntryModal({ nextId, onClose, onSave }: NewEntryModalProps) {
  const [date,        setDate]        = useState("Mar 28, 2026");
  const [reference,   setReference]   = useState("");
  const [description, setDescription] = useState("");
  const [category,    setCategory]    = useState("Expenses");
  const [lines,       setLines]       = useState<JournalLine[]>([
    { id: "l1", accountCode: "6101", accountName: "Office Expense",      description: "", debit: 0, credit: 0 },
    { id: "l2", accountCode: "1101", accountName: "Cash & Bank — Main",  description: "", debit: 0, credit: 0 },
  ]);

  const totalDebit  = lines.reduce((s, l) => s + l.debit,  0);
  const totalCredit = lines.reduce((s, l) => s + l.credit, 0);
  const isBalanced  = Math.abs(totalDebit - totalCredit) < 0.001 && totalDebit > 0;

  const updateLine = (id: string, field: keyof JournalLine, val: string | number) => {
    setLines(prev => prev.map(l => {
      if (l.id !== id) return l;
      if (field === "accountCode") {
        const acc = COA.find(a => a.code === (val as string));
        return { ...l, accountCode: val as string, accountName: acc?.name ?? "" };
      }
      return { ...l, [field]: val };
    }));
  };

  const addLine = () => {
    if (lines.length < 8) {
      setLines(prev => [...prev, { id: `l${Date.now()}`, accountCode: "6101", accountName: "Office Expense", description: "", debit: 0, credit: 0 }]);
    }
  };

  const removeLine = (id: string) => {
    if (lines.length > 2) setLines(prev => prev.filter(l => l.id !== id));
  };

  const handleSave = (asDraft: boolean) => {
    const entry: JournalEntry = {
      id: nextId, date, reference, description: description || "Manual journal entry",
      lines, totalDebit, totalCredit,
      status: asDraft ? "draft" : "posted",
      createdBy: "Ahmed Hassan",
      postedDate: asDraft ? undefined : date,
      category,
    };
    onSave(entry);
  };

  const inpStyle: React.CSSProperties = {
    width: "100%", padding: "8px 10px", borderRadius: 7,
    border: "1.5px solid #E2E8F0", fontSize: 12, color: "#334155",
    outline: "none", fontFamily: "'Inter', sans-serif", boxSizing: "border-box",
  };
  const labelStyle: React.CSSProperties = { color: "#374151", fontSize: 11.5, fontWeight: 600, display: "block", marginBottom: 5 };

  return (
    <div
      onClick={e => { if (e.target === e.currentTarget) onClose(); }}
      style={{ position: "fixed", inset: 0, backgroundColor: "rgba(15,23,42,0.65)", backdropFilter: "blur(4px)", zIndex: 100, display: "flex", alignItems: "center", justifyContent: "center", fontFamily: "'Inter', sans-serif" }}
    >
      <div style={{ width: 820, maxHeight: "92vh", backgroundColor: "#FFFFFF", borderRadius: 18, boxShadow: "0 32px 80px rgba(0,0,0,0.28)", display: "flex", flexDirection: "column", overflow: "hidden" }}>

        {/* Header */}
        <div style={{ padding: "18px 24px 14px", borderBottom: "1px solid #F1F5F9", display: "flex", alignItems: "center", justifyContent: "space-between", flexShrink: 0 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
            <div style={{ width: 40, height: 40, borderRadius: 10, background: "linear-gradient(135deg, #1E3A5F, #3B82F6)", display: "flex", alignItems: "center", justifyContent: "center" }}>
              <BookOpen size={18} color="white" />
            </div>
            <div>
              <div style={{ color: "#0F172A", fontSize: 15, fontWeight: 800 }}>New Journal Entry — قيد محاسبي</div>
              <div style={{ color: "#94A3B8", fontSize: 11, marginTop: 1 }}>Entry ID: {nextId}</div>
            </div>
          </div>
          <button onClick={onClose} style={{ width: 32, height: 32, borderRadius: 8, border: "none", backgroundColor: "#F1F5F9", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center" }}>
            <X size={15} style={{ color: "#64748B" }} />
          </button>
        </div>

        {/* Body */}
        <div style={{ flex: 1, overflowY: "auto", padding: "18px 24px" }}>

          {/* Row 1: Date, Reference, Category */}
          <div style={{ display: "flex", gap: 12, marginBottom: 14 }}>
            <div style={{ flex: 1 }}>
              <label style={labelStyle}>Date <span style={{ color: "#EF4444" }}>*</span></label>
              <input value={date} onChange={e => setDate(e.target.value)} style={inpStyle} />
            </div>
            <div style={{ flex: 1 }}>
              <label style={labelStyle}>Reference No.</label>
              <input placeholder="e.g. INV-2026-001" value={reference} onChange={e => setReference(e.target.value)} style={inpStyle} />
            </div>
            <div style={{ flex: 1 }}>
              <label style={labelStyle}>Category</label>
              <select value={category} onChange={e => setCategory(e.target.value)}
                style={{ ...inpStyle, cursor: "pointer" }}>
                {CATEGORIES.filter(c => c !== "All").map(c => <option key={c} value={c}>{c}</option>)}
              </select>
            </div>
          </div>

          {/* Row 2: Description */}
          <div style={{ marginBottom: 18 }}>
            <label style={labelStyle}>Description / البيان <span style={{ color: "#EF4444" }}>*</span></label>
            <input placeholder="Enter journal entry description…" value={description} onChange={e => setDescription(e.target.value)} style={inpStyle} />
          </div>

          {/* Journal Lines Table */}
          <div style={{ marginBottom: 14 }}>
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 8 }}>
              <label style={{ ...labelStyle, marginBottom: 0 }}>Journal Lines — بنود القيد <span style={{ color: "#EF4444" }}>*</span></label>
              <span style={{ color: "#94A3B8", fontSize: 11 }}>Min. 2 lines required</span>
            </div>

            <div style={{ border: "1.5px solid #E2E8F0", borderRadius: 10, overflow: "hidden" }}>
              {/* Header */}
              <div style={{ display: "grid", gridTemplateColumns: "180px 1fr 130px 130px 32px", backgroundColor: "#F8FAFC", padding: "8px 12px", borderBottom: "1px solid #E2E8F0", gap: 8 }}>
                {["Account", "Description / البيان", "Debit (مدين)", "Credit (دائن)", ""].map((h, i) => (
                  <div key={i} style={{ color: "#64748B", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.3px", textAlign: ["Debit (مدين)", "Credit (دائن)"].includes(h) ? "right" : "left" }}>{h}</div>
                ))}
              </div>

              {/* Lines */}
              {lines.map((line, idx) => (
                <div key={line.id} style={{ display: "grid", gridTemplateColumns: "180px 1fr 130px 130px 32px", padding: "8px 12px", borderBottom: idx < lines.length - 1 ? "1px solid #F1F5F9" : "none", alignItems: "center", gap: 8, backgroundColor: idx % 2 === 0 ? "#FFFFFF" : "#FAFBFC" }}>
                  {/* Account */}
                  <select value={line.accountCode}
                    onChange={e => updateLine(line.id, "accountCode", e.target.value)}
                    style={{ border: "1px solid #E2E8F0", borderRadius: 6, padding: "5px 6px", fontSize: 11, color: "#1E3A5F", fontWeight: 600, cursor: "pointer", outline: "none", fontFamily: "'Inter', sans-serif", width: "100%" }}>
                    {COA.map(a => <option key={a.code} value={a.code}>{a.code} — {a.name}</option>)}
                  </select>
                  {/* Description */}
                  <input placeholder="Line description…" value={line.description}
                    onChange={e => updateLine(line.id, "description", e.target.value)}
                    style={{ border: "1px solid #E2E8F0", borderRadius: 6, padding: "5px 8px", fontSize: 11.5, color: "#334155", outline: "none", fontFamily: "'Inter', sans-serif", width: "100%", boxSizing: "border-box" }} />
                  {/* Debit */}
                  <input type="number" min={0} value={line.debit || ""}
                    placeholder="0.00"
                    onChange={e => updateLine(line.id, "debit", parseFloat(e.target.value) || 0)}
                    style={{ border: "1px solid #E2E8F0", borderRadius: 6, padding: "5px 8px", fontSize: 12, color: line.debit > 0 ? "#1D4ED8" : "#94A3B8", fontWeight: line.debit > 0 ? 700 : 400, textAlign: "right", outline: "none", fontFamily: "'Inter', sans-serif", width: "100%", boxSizing: "border-box" }} />
                  {/* Credit */}
                  <input type="number" min={0} value={line.credit || ""}
                    placeholder="0.00"
                    onChange={e => updateLine(line.id, "credit", parseFloat(e.target.value) || 0)}
                    style={{ border: "1px solid #E2E8F0", borderRadius: 6, padding: "5px 8px", fontSize: 12, color: line.credit > 0 ? "#DC2626" : "#94A3B8", fontWeight: line.credit > 0 ? 700 : 400, textAlign: "right", outline: "none", fontFamily: "'Inter', sans-serif", width: "100%", boxSizing: "border-box" }} />
                  {/* Remove */}
                  <button onClick={() => removeLine(line.id)}
                    style={{ width: 26, height: 26, borderRadius: 6, border: "none", backgroundColor: "transparent", cursor: lines.length > 2 ? "pointer" : "default", display: "flex", alignItems: "center", justifyContent: "center", opacity: lines.length > 2 ? 1 : 0.3 }}>
                    <X size={12} style={{ color: "#CBD5E1" }} />
                  </button>
                </div>
              ))}

              {/* Totals row */}
              <div style={{ display: "grid", gridTemplateColumns: "180px 1fr 130px 130px 32px", padding: "9px 12px", backgroundColor: "#F8FAFC", borderTop: "2px solid #E2E8F0", gap: 8, alignItems: "center" }}>
                <div />
                <div style={{ color: "#64748B", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Totals</div>
                <div style={{ textAlign: "right", color: "#1D4ED8", fontSize: 13, fontWeight: 900 }}>
                  ${totalDebit.toLocaleString("en-US", { minimumFractionDigits: 2 })}
                </div>
                <div style={{ textAlign: "right", color: "#DC2626", fontSize: 13, fontWeight: 900 }}>
                  ${totalCredit.toLocaleString("en-US", { minimumFractionDigits: 2 })}
                </div>
                <div />
              </div>
            </div>

            <button onClick={addLine}
              style={{ marginTop: 8, display: "flex", alignItems: "center", gap: 5, padding: "6px 12px", borderRadius: 7, border: "1.5px dashed #CBD5E1", backgroundColor: "transparent", color: "#64748B", fontSize: 12, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
              <Plus size={12} /> Add Line
            </button>
          </div>

          {/* Balance Indicator */}
          <div style={{
            padding: "12px 16px", borderRadius: 10,
            backgroundColor: isBalanced ? "#ECFDF5" : (totalDebit === 0 && totalCredit === 0) ? "#F8FAFC" : "#FEF2F2",
            border: `1px solid ${isBalanced ? "#A7F3D0" : (totalDebit === 0 && totalCredit === 0) ? "#E2E8F0" : "#FECACA"}`,
            display: "flex", alignItems: "center", justifyContent: "space-between",
          }}>
            <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
              {isBalanced
                ? <CheckCircle size={15} style={{ color: "#10B981" }} />
                : <AlertTriangle size={15} style={{ color: totalDebit === 0 ? "#94A3B8" : "#EF4444" }} />
              }
              <div>
                <div style={{ fontSize: 12.5, fontWeight: 700, color: isBalanced ? "#065F46" : totalDebit === 0 ? "#64748B" : "#991B1B" }}>
                  {isBalanced ? "Entry is Balanced — القيد متوازن ✓" : totalDebit === 0 ? "Enter amounts to validate" : "Entry is NOT balanced — القيد غير متوازن!"}
                </div>
                {!isBalanced && totalDebit > 0 && (
                  <div style={{ fontSize: 11, color: "#EF4444", marginTop: 2 }}>
                    Difference: ${Math.abs(totalDebit - totalCredit).toLocaleString("en-US", { minimumFractionDigits: 2 })}
                  </div>
                )}
              </div>
            </div>
            <div style={{ textAlign: "right" }}>
              <div style={{ fontSize: 11, color: "#94A3B8" }}>Debit / Credit</div>
              <div style={{ fontSize: 14, fontWeight: 800, color: isBalanced ? "#065F46" : "#991B1B" }}>
                ${totalDebit.toLocaleString("en-US", { minimumFractionDigits: 2 })} / ${totalCredit.toLocaleString("en-US", { minimumFractionDigits: 2 })}
              </div>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div style={{ padding: "14px 24px", borderTop: "1px solid #F1F5F9", display: "flex", gap: 10, justifyContent: "flex-end", flexShrink: 0 }}>
          <button onClick={onClose} style={{ padding: "9px 20px", borderRadius: 9, border: "1.5px solid #E2E8F0", backgroundColor: "#FFFFFF", color: "#64748B", fontSize: 13, fontWeight: 600, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>Cancel</button>
          <button onClick={() => handleSave(true)} style={{ padding: "9px 18px", borderRadius: 9, border: "1.5px solid #1E3A5F", backgroundColor: "#FFFFFF", color: "#1E3A5F", fontSize: 13, fontWeight: 600, cursor: "pointer", fontFamily: "'Inter', sans-serif", display: "flex", alignItems: "center", gap: 6 }}>
            <FileText size={13} /> Save Draft
          </button>
          <button onClick={() => handleSave(false)} disabled={!isBalanced}
            style={{ padding: "9px 22px", borderRadius: 9, border: "none", backgroundColor: isBalanced ? "#1E3A5F" : "#94A3B8", color: "#FFFFFF", fontSize: 13, fontWeight: 700, cursor: isBalanced ? "pointer" : "not-allowed", fontFamily: "'Inter', sans-serif", display: "flex", alignItems: "center", gap: 7 }}>
            <Stamp size={13} /> Post Entry
          </button>
        </div>
      </div>
    </div>
  );
}

// ─── Entry Detail Panel ───────────────────────────────────────────────────────

function EntryDetail({ entry, onPost, onReverse }: {
  entry: JournalEntry;
  onPost: (id: string) => void;
  onReverse: (id: string) => void;
}) {
  const sc = STATUS_CFG[entry.status];

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 12, fontFamily: "'Inter', sans-serif" }}>

      {/* Header */}
      <div style={{ background: "linear-gradient(135deg, #1E3A5F, #2D5FA8)", borderRadius: 12, padding: 18, boxShadow: "0 4px 16px rgba(30,58,95,0.25)" }}>
        <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", marginBottom: 12 }}>
          <div>
            <div style={{ color: "rgba(255,255,255,0.5)", fontSize: 9.5, fontWeight: 600, letterSpacing: "0.6px", textTransform: "uppercase", marginBottom: 4 }}>Journal Entry — قيد محاسبي</div>
            <div style={{ color: "#FFFFFF", fontSize: 14, fontWeight: 800, fontFamily: "monospace" }}>{entry.id}</div>
            <div style={{ color: "rgba(255,255,255,0.7)", fontSize: 11, marginTop: 3 }}>{entry.date}</div>
          </div>
          <div style={{ display: "flex", flexDirection: "column", alignItems: "flex-end", gap: 5 }}>
            <span style={{ backgroundColor: sc.bg, color: sc.color, fontSize: 10, fontWeight: 700, padding: "3px 9px", borderRadius: 12, border: `1px solid ${sc.border}`, display: "flex", alignItems: "center", gap: 4 }}>
              <span style={{ width: 5, height: 5, borderRadius: "50%", backgroundColor: sc.dot, display: "inline-block" }} />
              {sc.label}
            </span>
            <CatBadge category={entry.category} />
          </div>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 8 }}>
          {[
            { label: "Total Debit",    value: `$${entry.totalDebit.toLocaleString("en-US", { minimumFractionDigits: 2 })}`,  color: "#93C5FD" },
            { label: "Total Credit",   value: `$${entry.totalCredit.toLocaleString("en-US", { minimumFractionDigits: 2 })}`, color: "#FCA5A5" },
            { label: "Lines",          value: `${entry.lines.length} entries`, color: "rgba(255,255,255,0.85)" },
            { label: "Created By",     value: entry.createdBy,                 color: "rgba(255,255,255,0.85)" },
          ].map(m => (
            <div key={m.label} style={{ backgroundColor: "rgba(255,255,255,0.08)", borderRadius: 8, padding: "8px 10px" }}>
              <div style={{ color: "rgba(255,255,255,0.4)", fontSize: 9.5, marginBottom: 3, textTransform: "uppercase", letterSpacing: "0.4px" }}>{m.label}</div>
              <div style={{ color: m.color, fontSize: 12, fontWeight: 700 }}>{m.value}</div>
            </div>
          ))}
        </div>
      </div>

      {/* Description */}
      <div style={{ backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: 12, padding: 14, boxShadow: "0 1px 4px rgba(0,0,0,0.05)" }}>
        <div style={{ color: "#64748B", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.6px", marginBottom: 8 }}>Description / البيان</div>
        <p style={{ margin: 0, color: "#334155", fontSize: 12.5, lineHeight: 1.5 }}>{entry.description}</p>
        {entry.reference && (
          <div style={{ marginTop: 8, display: "flex", alignItems: "center", gap: 5 }}>
            <span style={{ color: "#94A3B8", fontSize: 11 }}>Reference:</span>
            <span style={{ color: "#3B82F6", fontSize: 11, fontWeight: 700, fontFamily: "monospace" }}>{entry.reference}</span>
          </div>
        )}
        {entry.postedDate && (
          <div style={{ marginTop: 4, display: "flex", alignItems: "center", gap: 5 }}>
            <span style={{ color: "#94A3B8", fontSize: 11 }}>Posted:</span>
            <span style={{ color: "#10B981", fontSize: 11, fontWeight: 600 }}>{entry.postedDate}</span>
          </div>
        )}
      </div>

      {/* Journal Lines — T-Account Style */}
      <div style={{ backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: 12, padding: 14, boxShadow: "0 1px 4px rgba(0,0,0,0.05)" }}>
        <div style={{ color: "#64748B", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.6px", marginBottom: 10 }}>
          Entry Lines — بنود القيد
        </div>
        <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 11 }}>
          <thead>
            <tr style={{ backgroundColor: "#F8FAFC" }}>
              <th style={{ padding: "6px 8px", textAlign: "left",  color: "#64748B", fontSize: 9.5, fontWeight: 700, textTransform: "uppercase", borderBottom: "1px solid #E2E8F0" }}>Account</th>
              <th style={{ padding: "6px 8px", textAlign: "right", color: "#1D4ED8", fontSize: 9.5, fontWeight: 700, textTransform: "uppercase", borderBottom: "1px solid #E2E8F0" }}>Debit</th>
              <th style={{ padding: "6px 8px", textAlign: "right", color: "#DC2626", fontSize: 9.5, fontWeight: 700, textTransform: "uppercase", borderBottom: "1px solid #E2E8F0" }}>Credit</th>
            </tr>
          </thead>
          {entry.lines.map((line, idx) => (
            <tbody key={line.id}>
              <tr style={{ borderBottom: idx < entry.lines.length - 1 ? "1px solid #F1F5F9" : "none" }}>
                <td style={{ padding: "8px 8px" }}>
                  <div style={{ display: "flex", alignItems: "center", gap: 5 }}>
                    {line.debit > 0 && <div style={{ width: 3, height: 20, backgroundColor: "#1D4ED8", borderRadius: 2, flexShrink: 0 }} />}
                    {line.credit > 0 && <div style={{ width: 3, height: 20, backgroundColor: "#DC2626", borderRadius: 2, flexShrink: 0 }} />}
                    <div>
                      <div style={{ color: "#1E3A5F", fontSize: 11, fontWeight: 700 }}>{line.accountCode}</div>
                      <div style={{ color: "#64748B", fontSize: 10, marginTop: 1 }}>{line.accountName}</div>
                    </div>
                  </div>
                </td>
                <td style={{ padding: "8px 8px", textAlign: "right" }}>
                  {line.debit > 0
                    ? <span style={{ color: "#1D4ED8", fontWeight: 800, fontSize: 12 }}>${line.debit.toLocaleString("en-US", { minimumFractionDigits: 2 })}</span>
                    : <span style={{ color: "#CBD5E1", fontSize: 11 }}>—</span>
                  }
                </td>
                <td style={{ padding: "8px 8px", textAlign: "right" }}>
                  {line.credit > 0
                    ? <span style={{ color: "#DC2626", fontWeight: 800, fontSize: 12 }}>${line.credit.toLocaleString("en-US", { minimumFractionDigits: 2 })}</span>
                    : <span style={{ color: "#CBD5E1", fontSize: 11 }}>—</span>
                  }
                </td>
              </tr>
            </tbody>
          ))}
          <tbody>
            <tr style={{ backgroundColor: "#F8FAFC" }}>
              <td style={{ padding: "7px 8px", color: "#1E3A5F", fontSize: 11, fontWeight: 800 }}>Total</td>
              <td style={{ padding: "7px 8px", textAlign: "right", color: "#1D4ED8", fontSize: 12.5, fontWeight: 900 }}>
                ${entry.totalDebit.toLocaleString("en-US", { minimumFractionDigits: 2 })}
              </td>
              <td style={{ padding: "7px 8px", textAlign: "right", color: "#DC2626", fontSize: 12.5, fontWeight: 900 }}>
                ${entry.totalCredit.toLocaleString("en-US", { minimumFractionDigits: 2 })}
              </td>
            </tr>
          </tbody>
        </table>
        <div style={{ marginTop: 8, display: "flex", alignItems: "center", gap: 5, color: "#10B981", fontSize: 10.5, fontWeight: 600 }}>
          <CheckCircle size={11} /> Balanced — المدين يساوي الدائن
        </div>
      </div>

      {/* Actions */}
      <div style={{ display: "flex", gap: 8 }}>
        {entry.status === "draft" && (
          <button onClick={() => onPost(entry.id)}
            style={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center", gap: 6, padding: "9px 0", borderRadius: 9, border: "none", backgroundColor: "#1E3A5F", color: "#FFFFFF", fontSize: 12, fontWeight: 700, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <Stamp size={13} /> Post Entry
          </button>
        )}
        {entry.status === "posted" && (
          <button onClick={() => onReverse(entry.id)}
            style={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center", gap: 6, padding: "9px 0", borderRadius: 9, border: "1.5px solid #E2E8F0", backgroundColor: "#FFFFFF", color: "#64748B", fontSize: 12, fontWeight: 600, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <RotateCcw size={13} /> Reverse Entry
          </button>
        )}
        <button style={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center", gap: 6, padding: "9px 0", borderRadius: 9, border: "1px solid #E2E8F0", backgroundColor: "#F8FAFC", color: "#64748B", fontSize: 12, fontWeight: 600, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
          <Printer size={13} /> Print
        </button>
      </div>
    </div>
  );
}

// ─── Main Component ───────────────────────────────────────────────────────────

export function JournalTab() {
  const [entries,      setEntries]      = useState<JournalEntry[]>(SEED_ENTRIES);
  const [search,       setSearch]       = useState("");
  const [statusFilter, setStatusFilter] = useState("All");
  const [catFilter,    setCatFilter]    = useState("All");
  const [selectedId,   setSelectedId]   = useState<string>(SEED_ENTRIES[0].id);
  const [showModal,    setShowModal]    = useState(false);

  const selected = entries.find(e => e.id === selectedId) ?? entries[0];

  const filtered = useMemo(() => {
    return entries.filter(e => {
      const matchStatus = statusFilter === "All" || e.status === statusFilter;
      const matchCat    = catFilter    === "All" || e.category === catFilter;
      const q           = search.toLowerCase();
      const matchSearch = !q || e.id.toLowerCase().includes(q) || e.description.toLowerCase().includes(q) || e.reference.toLowerCase().includes(q) || e.createdBy.toLowerCase().includes(q);
      return matchStatus && matchCat && matchSearch;
    });
  }, [entries, search, statusFilter, catFilter]);

  // KPIs
  const totalEntries  = entries.length;
  const postedCount   = entries.filter(e => e.status === "posted").length;
  const draftCount    = entries.filter(e => e.status === "draft").length;
  const totalDebitAll = entries.filter(e => e.status === "posted").reduce((s, e) => s + e.totalDebit, 0);

  const kpis = [
    { title: "Total Entries",    value: `${totalEntries}`,                                   sub: "All journal entries",   color: "#3B82F6", bg: "#EFF6FF", icon: <BookOpen size={18} /> },
    { title: "Posted Entries",   value: `${postedCount}`,                                    sub: "Approved & in ledger",  color: "#10B981", bg: "#ECFDF5", icon: <CheckCircle size={18} /> },
    { title: "Draft Entries",    value: `${draftCount}`,                                     sub: "Pending review",        color: "#F59E0B", bg: "#FFFBEB", icon: <Clock size={18} /> },
    { title: "Total Posted",     value: `$${(totalDebitAll / 1000).toFixed(0)}k`,            sub: "Cumulative debit value", color: "#8B5CF6", bg: "#F5F3FF", icon: <Layers size={18} /> },
  ];

  const nextId = `JE-2026-0${(entries.length + 1).toString().padStart(2, "0")}`;

  const handleSave    = (e: JournalEntry) => { setEntries(prev => [e, ...prev]); setSelectedId(e.id); setShowModal(false); };
  const handlePost    = (id: string) => setEntries(prev => prev.map(e => e.id === id ? { ...e, status: "posted", postedDate: "Mar 28, 2026" } : e));
  const handleReverse = (id: string) => {
    const orig = entries.find(e => e.id === id);
    if (!orig) return;
    const reversedId = `JE-2026-REV-${id.split("-").pop()}`;
    const reversedEntry: JournalEntry = {
      ...orig,
      id: reversedId,
      description: `Reversal: ${orig.description}`,
      reference: `REV-${orig.id}`,
      status: "reversed",
      postedDate: "Mar 28, 2026",
      lines: orig.lines.map(l => ({ ...l, id: `${l.id}-rev`, debit: l.credit, credit: l.debit })),
      category: "Reversal",
    };
    setEntries(prev => [reversedEntry, ...prev]);
    setSelectedId(reversedId);
  };

  const statusCount = (s: string) => s === "All" ? entries.length : entries.filter(e => e.status === s).length;

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 16, fontFamily: "'Inter', sans-serif" }}>

      {/* ── Page Header ── */}
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div>
          <h2 style={{ margin: 0, color: "#0F172A", fontSize: 16, fontWeight: 800 }}>Journal Entries — القيود المحا��بية</h2>
          <p style={{ margin: "2px 0 0", color: "#94A3B8", fontSize: 12 }}>General ledger journal entries with debit/credit lines, balance validation & reversal · FY 2025–26</p>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <button style={{ display: "flex", alignItems: "center", gap: 6, padding: "8px 14px", borderRadius: 9, border: "1px solid #E2E8F0", backgroundColor: "#FFFFFF", color: "#64748B", fontSize: 12, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <Download size={13} /> Export
          </button>
          <button onClick={() => setShowModal(true)}
            style={{ display: "flex", alignItems: "center", gap: 6, padding: "8px 16px", borderRadius: 9, border: "none", backgroundColor: "#1E3A5F", color: "#FFFFFF", fontSize: 12, fontWeight: 700, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <Plus size={14} /> New Entry
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

        {/* ── Left: Entries Table ── */}
        <div style={{ flex: 1, backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: 12, boxShadow: "0 1px 4px rgba(0,0,0,0.05)", overflow: "hidden", minWidth: 0 }}>

          {/* Toolbar */}
          <div style={{ padding: "12px 18px", borderBottom: "1px solid #F1F5F9", display: "flex", alignItems: "center", justifyContent: "space-between", gap: 10, flexWrap: "wrap" }}>
            {/* Status tabs */}
            <div style={{ display: "flex", gap: 2, backgroundColor: "#F5F7FA", padding: 3, borderRadius: 10, flexShrink: 0 }}>
              {[
                { key: "All",      label: "All" },
                { key: "posted",   label: "Posted" },
                { key: "draft",    label: "Draft" },
                { key: "reversed", label: "Reversed" },
              ].map(tab => (
                <button key={tab.key} onClick={() => setStatusFilter(tab.key)}
                  style={{ padding: "4px 10px", borderRadius: 7, border: "none", backgroundColor: statusFilter === tab.key ? "#FFFFFF" : "transparent", color: statusFilter === tab.key ? "#1E3A5F" : "#64748B", fontSize: 11, fontWeight: statusFilter === tab.key ? 700 : 400, cursor: "pointer", boxShadow: statusFilter === tab.key ? "0 1px 3px rgba(0,0,0,0.1)" : "none", fontFamily: "'Inter', sans-serif", display: "flex", alignItems: "center", gap: 4 }}>
                  {tab.label}
                  <span style={{ backgroundColor: statusFilter === tab.key ? "#EFF6FF" : "#E2E8F0", color: statusFilter === tab.key ? "#1E3A5F" : "#94A3B8", fontSize: 9.5, fontWeight: 700, padding: "1px 5px", borderRadius: 8 }}>
                    {statusCount(tab.key)}
                  </span>
                </button>
              ))}
            </div>
            <div style={{ display: "flex", gap: 8 }}>
              {/* Search */}
              <div style={{ display: "flex", alignItems: "center", gap: 7, padding: "6px 12px", borderRadius: 8, border: "1px solid #E2E8F0", backgroundColor: "#F8FAFC", width: 180 }}>
                <Search size={12} style={{ color: "#94A3B8", flexShrink: 0 }} />
                <input type="text" placeholder="Search entries…" value={search} onChange={e => setSearch(e.target.value)}
                  style={{ flex: 1, border: "none", outline: "none", background: "transparent", color: "#334155", fontSize: 12, fontFamily: "'Inter', sans-serif" }} />
              </div>
              {/* Category filter */}
              <select value={catFilter} onChange={e => setCatFilter(e.target.value)}
                style={{ padding: "6px 10px", borderRadius: 8, border: "1px solid #E2E8F0", backgroundColor: "#F8FAFC", color: "#64748B", fontSize: 11, cursor: "pointer", fontFamily: "'Inter', sans-serif", outline: "none" }}>
                {CATEGORIES.map(c => <option key={c} value={c}>{c}</option>)}
              </select>
            </div>
          </div>

          {/* Table */}
          <div style={{ overflowY: "auto", maxHeight: 400 }}>
            <table style={{ width: "100%", borderCollapse: "collapse" }}>
              <thead style={{ position: "sticky", top: 0, zIndex: 2 }}>
                <tr style={{ backgroundColor: "#F8FAFC" }}>
                  {["Entry #", "Date", "Description / البيان", "Category", "Lines", "Debit", "Credit", "Status"].map(h => (
                    <th key={h} style={{ padding: "8px 12px", textAlign: ["Debit", "Credit", "Lines"].includes(h) ? "right" : "left", color: "#64748B", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.4px", borderBottom: "1px solid #E2E8F0", backgroundColor: "#F8FAFC", whiteSpace: "nowrap" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {filtered.map((entry, idx) => {
                  const isSelected = selectedId === entry.id;
                  return (
                    <tr key={entry.id}
                      onClick={() => setSelectedId(entry.id)}
                      style={{ backgroundColor: isSelected ? "#EFF6FF" : idx % 2 === 0 ? "#FFFFFF" : "#FAFBFC", borderBottom: "1px solid #F1F5F9", cursor: "pointer", borderLeft: isSelected ? "3px solid #3B82F6" : "3px solid transparent" }}
                      onMouseEnter={e => { if (!isSelected) (e.currentTarget as HTMLTableRowElement).style.backgroundColor = "#F8FAFC"; }}
                      onMouseLeave={e => { if (!isSelected) (e.currentTarget as HTMLTableRowElement).style.backgroundColor = idx % 2 === 0 ? "#FFFFFF" : "#FAFBFC"; }}>
                      <td style={{ padding: "10px 12px" }}>
                        <span style={{ color: "#3B82F6", fontSize: 11, fontWeight: 700, fontFamily: "monospace" }}>{entry.id}</span>
                      </td>
                      <td style={{ padding: "10px 12px", color: "#64748B", fontSize: 11.5, whiteSpace: "nowrap" }}>{entry.date}</td>
                      <td style={{ padding: "10px 12px", maxWidth: 240 }}>
                        <div style={{ color: "#0F172A", fontSize: 12, fontWeight: 600, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{entry.description}</div>
                        {entry.reference && <div style={{ color: "#94A3B8", fontSize: 10, marginTop: 1, fontFamily: "monospace" }}>Ref: {entry.reference}</div>}
                      </td>
                      <td style={{ padding: "10px 12px" }}><CatBadge category={entry.category} /></td>
                      <td style={{ padding: "10px 12px", textAlign: "right" }}>
                        <span style={{ color: "#64748B", fontSize: 12, fontWeight: 600 }}>{entry.lines.length}</span>
                      </td>
                      <td style={{ padding: "10px 12px", textAlign: "right", whiteSpace: "nowrap" }}>
                        <span style={{ color: "#1D4ED8", fontSize: 12.5, fontWeight: 800 }}>${entry.totalDebit.toLocaleString()}</span>
                      </td>
                      <td style={{ padding: "10px 12px", textAlign: "right", whiteSpace: "nowrap" }}>
                        <span style={{ color: "#DC2626", fontSize: 12.5, fontWeight: 800 }}>${entry.totalCredit.toLocaleString()}</span>
                      </td>
                      <td style={{ padding: "10px 12px" }}><StatusBadge status={entry.status} /></td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
            {filtered.length === 0 && (
              <div style={{ padding: "40px 0", textAlign: "center", color: "#94A3B8", fontSize: 13 }}>No journal entries found</div>
            )}
          </div>

          {/* Footer */}
          <div style={{ padding: "10px 18px", borderTop: "1px solid #F1F5F9", display: "flex", justifyContent: "space-between", alignItems: "center", backgroundColor: "#FAFBFC" }}>
            <span style={{ color: "#94A3B8", fontSize: 11 }}>{filtered.length} entr{filtered.length !== 1 ? "ies" : "y"} shown</span>
            <div style={{ display: "flex", gap: 20 }}>
              <div style={{ display: "flex", gap: 4, alignItems: "center" }}>
                <span style={{ fontSize: 10, color: "#94A3B8" }}>Total Debit:</span>
                <span style={{ fontSize: 12, color: "#1D4ED8", fontWeight: 800 }}>${filtered.reduce((s, e) => s + e.totalDebit, 0).toLocaleString()}</span>
              </div>
              <div style={{ display: "flex", gap: 4, alignItems: "center" }}>
                <span style={{ fontSize: 10, color: "#94A3B8" }}>Total Credit:</span>
                <span style={{ fontSize: 12, color: "#DC2626", fontWeight: 800 }}>${filtered.reduce((s, e) => s + e.totalCredit, 0).toLocaleString()}</span>
              </div>
            </div>
          </div>
        </div>

        {/* ── Right: Detail Panel ── */}
        <div style={{ width: 290, flexShrink: 0, overflowY: "auto", maxHeight: 560 }}>
          {selected && (
            <EntryDetail
              entry={selected}
              onPost={handlePost}
              onReverse={handleReverse}
            />
          )}
        </div>
      </div>

      {showModal && (
        <NewEntryModal
          nextId={nextId}
          onClose={() => setShowModal(false)}
          onSave={handleSave}
        />
      )}
    </div>
  );
}
