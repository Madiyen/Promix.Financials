import { Eye, Pencil, MoreHorizontal, Download, Filter, ChevronDown } from "lucide-react";

interface Transaction {
  id: string;
  date: string;
  party: string;
  partyType: "customer" | "vendor";
  type: string;
  amount: number;
  status: "paid" | "unpaid" | "partial";
  reference: string;
}

const transactions: Transaction[] = [
  { id: "INV-2024", date: "Feb 24, 2026", party: "TechCorp Solutions", partyType: "customer", type: "Sales Invoice", amount: 8750.0, status: "paid", reference: "PO-8821" },
  { id: "INV-2023", date: "Feb 23, 2026", party: "Global Imports LLC", partyType: "vendor", type: "Purchase Invoice", amount: 3420.5, status: "unpaid", reference: "PO-8820" },
  { id: "RV-0891", date: "Feb 23, 2026", party: "Sunrise Retail Co.", partyType: "customer", type: "Receipt Voucher", amount: 12000.0, status: "paid", reference: "INV-2019" },
  { id: "INV-2022", date: "Feb 22, 2026", party: "Al-Farhan Trading", partyType: "customer", type: "Sales Invoice", amount: 5600.0, status: "partial", reference: "PO-8815" },
  { id: "JE-0445", date: "Feb 22, 2026", party: "Internal — Depreciation", partyType: "vendor", type: "Journal Entry", amount: 2100.0, status: "paid", reference: "AUTO" },
  { id: "INV-2021", date: "Feb 21, 2026", party: "NextGen Supplies", partyType: "vendor", type: "Purchase Invoice", amount: 9340.0, status: "paid", reference: "PO-8810" },
  { id: "INV-2020", date: "Feb 20, 2026", party: "Orion Healthcare", partyType: "customer", type: "Sales Invoice", amount: 4280.0, status: "unpaid", reference: "PO-8808" },
  { id: "RV-0890", date: "Feb 19, 2026", party: "Summit Builders", partyType: "customer", type: "Receipt Voucher", amount: 15500.0, status: "partial", reference: "INV-2015" },
];

const StatusBadge = ({ status }: { status: "paid" | "unpaid" | "partial" }) => {
  const cfg = {
    paid: { bg: "#ECFDF5", color: "#065F46", dot: "#10B981", label: "Paid" },
    unpaid: { bg: "#FEF2F2", color: "#991B1B", dot: "#EF4444", label: "Unpaid" },
    partial: { bg: "#FFFBEB", color: "#92400E", dot: "#F59E0B", label: "Partial" },
  }[status];

  return (
    <span
      className="flex items-center gap-1.5 px-2.5 py-1 rounded-full"
      style={{ backgroundColor: cfg.bg, display: "inline-flex", fontSize: "11px", fontWeight: 600, color: cfg.color }}
    >
      <span style={{ width: "6px", height: "6px", borderRadius: "50%", backgroundColor: cfg.dot, display: "inline-block" }} />
      {cfg.label}
    </span>
  );
};

const TypeBadge = ({ type }: { type: string }) => {
  const colors: Record<string, { bg: string; color: string }> = {
    "Sales Invoice": { bg: "#EFF6FF", color: "#1D4ED8" },
    "Purchase Invoice": { bg: "#F5F3FF", color: "#5B21B6" },
    "Receipt Voucher": { bg: "#ECFDF5", color: "#065F46" },
    "Journal Entry": { bg: "#FFF7ED", color: "#92400E" },
  };
  const cfg = colors[type] || { bg: "#F8FAFC", color: "#64748B" };
  return (
    <span
      style={{
        backgroundColor: cfg.bg,
        color: cfg.color,
        fontSize: "11px",
        fontWeight: 500,
        padding: "3px 8px",
        borderRadius: "5px",
        display: "inline-block",
      }}
    >
      {type}
    </span>
  );
};

export function TransactionsTable() {
  const totalAmount = transactions.reduce((sum, t) => sum + t.amount, 0);

  return (
    <div
      className="rounded-xl overflow-hidden"
      style={{
        backgroundColor: "#FFFFFF",
        boxShadow: "0 1px 4px rgba(0,0,0,0.06), 0 4px 16px rgba(0,0,0,0.04)",
        border: "1px solid #F1F5F9",
        fontFamily: "'Inter', sans-serif",
      }}
    >
      {/* Table Header */}
      <div
        className="flex items-center justify-between px-5 py-4"
        style={{ borderBottom: "1px solid #F1F5F9" }}
      >
        <div>
          <h3 style={{ color: "#0F172A", fontSize: "14px", fontWeight: 700, margin: 0 }}>
            Recent Transactions
          </h3>
          <p style={{ color: "#94A3B8", fontSize: "11.5px", margin: "2px 0 0" }}>
            Showing {transactions.length} latest transactions
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            className="flex items-center gap-1.5 px-3 py-2 rounded-lg"
            style={{ backgroundColor: "#F1F5F9", border: "1px solid #E2E8F0", cursor: "pointer", color: "#64748B", fontSize: "12px" }}
          >
            <Filter size={13} />
            Filter
            <ChevronDown size={12} />
          </button>
          <button
            className="flex items-center gap-1.5 px-3 py-2 rounded-lg"
            style={{ backgroundColor: "#F1F5F9", border: "1px solid #E2E8F0", cursor: "pointer", color: "#64748B", fontSize: "12px" }}
          >
            <Download size={13} />
            Export
          </button>
          <button
            className="flex items-center gap-1.5 px-4 py-2 rounded-lg"
            style={{ backgroundColor: "#1E3A5F", border: "none", cursor: "pointer", color: "#FFFFFF", fontSize: "12px", fontWeight: 600 }}
          >
            View All
          </button>
        </div>
      </div>

      {/* Table */}
      <div style={{ overflowX: "auto" }}>
        <table style={{ width: "100%", borderCollapse: "collapse" }}>
          <thead>
            <tr style={{ backgroundColor: "#F8FAFC" }}>
              {["#", "Date", "Customer / Vendor", "Type", "Reference", "Amount", "Status", "Actions"].map((h) => (
                <th
                  key={h}
                  style={{
                    padding: "10px 16px",
                    textAlign: h === "Amount" ? "right" : "left",
                    color: "#64748B",
                    fontSize: "11px",
                    fontWeight: 700,
                    textTransform: "uppercase",
                    letterSpacing: "0.6px",
                    borderBottom: "1px solid #E2E8F0",
                    whiteSpace: "nowrap",
                  }}
                >
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {transactions.map((tx, idx) => (
              <tr
                key={tx.id}
                style={{
                  backgroundColor: idx % 2 === 0 ? "#FFFFFF" : "#FAFBFC",
                  borderBottom: "1px solid #F1F5F9",
                  transition: "background-color 0.1s",
                }}
                onMouseEnter={(e) => {
                  (e.currentTarget as HTMLTableRowElement).style.backgroundColor = "#F0F7FF";
                }}
                onMouseLeave={(e) => {
                  (e.currentTarget as HTMLTableRowElement).style.backgroundColor = idx % 2 === 0 ? "#FFFFFF" : "#FAFBFC";
                }}
              >
                <td style={{ padding: "11px 16px" }}>
                  <span style={{ color: "#3B82F6", fontSize: "12.5px", fontWeight: 700 }}>{tx.id}</span>
                </td>
                <td style={{ padding: "11px 16px" }}>
                  <span style={{ color: "#64748B", fontSize: "12.5px" }}>{tx.date}</span>
                </td>
                <td style={{ padding: "11px 16px" }}>
                  <div className="flex items-center gap-2.5">
                    <div
                      className="flex items-center justify-center rounded-lg"
                      style={{
                        width: "30px",
                        height: "30px",
                        backgroundColor: tx.partyType === "customer" ? "#EFF6FF" : "#F5F3FF",
                        flexShrink: 0,
                      }}
                    >
                      <span
                        style={{
                          color: tx.partyType === "customer" ? "#3B82F6" : "#8B5CF6",
                          fontSize: "11px",
                          fontWeight: 700,
                        }}
                      >
                        {tx.party.split(" ").map((w) => w[0]).slice(0, 2).join("")}
                      </span>
                    </div>
                    <div>
                      <div style={{ color: "#0F172A", fontSize: "12.5px", fontWeight: 600 }}>{tx.party}</div>
                      <div style={{ color: "#94A3B8", fontSize: "10.5px", textTransform: "capitalize" }}>
                        {tx.partyType}
                      </div>
                    </div>
                  </div>
                </td>
                <td style={{ padding: "11px 16px" }}>
                  <TypeBadge type={tx.type} />
                </td>
                <td style={{ padding: "11px 16px" }}>
                  <span style={{ color: "#94A3B8", fontSize: "12px", fontFamily: "monospace" }}>{tx.reference}</span>
                </td>
                <td style={{ padding: "11px 16px", textAlign: "right" }}>
                  <span
                    style={{
                      color: tx.partyType === "customer" ? "#10B981" : "#EF4444",
                      fontSize: "13px",
                      fontWeight: 700,
                    }}
                  >
                    {tx.partyType === "vendor" ? "−" : "+"}${tx.amount.toLocaleString("en-US", { minimumFractionDigits: 2 })}
                  </span>
                </td>
                <td style={{ padding: "11px 16px" }}>
                  <StatusBadge status={tx.status} />
                </td>
                <td style={{ padding: "11px 16px" }}>
                  <div className="flex items-center gap-1.5">
                    <button
                      className="flex items-center justify-center rounded-lg"
                      style={{
                        width: "28px",
                        height: "28px",
                        backgroundColor: "#EFF6FF",
                        border: "none",
                        cursor: "pointer",
                      }}
                      title="View"
                    >
                      <Eye size={13} style={{ color: "#3B82F6" }} />
                    </button>
                    <button
                      className="flex items-center justify-center rounded-lg"
                      style={{
                        width: "28px",
                        height: "28px",
                        backgroundColor: "#F5F3FF",
                        border: "none",
                        cursor: "pointer",
                      }}
                      title="Edit"
                    >
                      <Pencil size={13} style={{ color: "#8B5CF6" }} />
                    </button>
                    <button
                      className="flex items-center justify-center rounded-lg"
                      style={{
                        width: "28px",
                        height: "28px",
                        backgroundColor: "#F8FAFC",
                        border: "none",
                        cursor: "pointer",
                      }}
                      title="More"
                    >
                      <MoreHorizontal size={13} style={{ color: "#94A3B8" }} />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr style={{ backgroundColor: "#F8FAFC", borderTop: "2px solid #E2E8F0" }}>
              <td colSpan={5} style={{ padding: "12px 16px", color: "#64748B", fontSize: "12.5px", fontWeight: 700 }}>
                Total ({transactions.length} transactions)
              </td>
              <td style={{ padding: "12px 16px", textAlign: "right", color: "#0F172A", fontSize: "14px", fontWeight: 800 }}>
                ${totalAmount.toLocaleString("en-US", { minimumFractionDigits: 2 })}
              </td>
              <td colSpan={2} />
            </tr>
          </tfoot>
        </table>
      </div>

      {/* Pagination */}
      <div
        className="flex items-center justify-between px-5 py-3"
        style={{ borderTop: "1px solid #F1F5F9" }}
      >
        <span style={{ color: "#94A3B8", fontSize: "12px" }}>
          Showing 1–8 of 284 entries
        </span>
        <div className="flex items-center gap-1">
          {["‹ Prev", "1", "2", "3", "...", "36", "Next ›"].map((p, i) => (
            <button
              key={i}
              style={{
                minWidth: "30px",
                height: "30px",
                borderRadius: "7px",
                border: p === "1" ? "none" : "1px solid #E2E8F0",
                backgroundColor: p === "1" ? "#1E3A5F" : "transparent",
                color: p === "1" ? "#FFFFFF" : "#64748B",
                fontSize: "12px",
                fontWeight: p === "1" ? 700 : 400,
                cursor: "pointer",
                padding: "0 8px",
                fontFamily: "'Inter', sans-serif",
              }}
            >
              {p}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}
