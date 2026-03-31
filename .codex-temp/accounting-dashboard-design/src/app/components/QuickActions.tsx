import React from "react";
import { FilePlus, Receipt, BookOpen, UserPlus, Download, Upload } from "lucide-react";

interface ActionBtnProps {
  icon: React.ReactNode;
  label: string;
  sublabel: string;
  color: string;
  bgColor: string;
  borderColor: string;
  primary?: boolean;
}

function ActionBtn({ icon, label, sublabel, color, bgColor, borderColor, primary }: ActionBtnProps) {
  return (
    <button
      className="flex items-center gap-3 px-5 py-3.5 rounded-xl transition-all duration-150 flex-1"
      style={{
        backgroundColor: primary ? "#1E3A5F" : bgColor,
        border: `1.5px solid ${primary ? "#1E3A5F" : borderColor}`,
        cursor: "pointer",
        fontFamily: "'Inter', sans-serif",
        boxShadow: primary ? "0 4px 12px rgba(30,58,95,0.25)" : "0 1px 3px rgba(0,0,0,0.04)",
      }}
      onMouseEnter={(e) => {
        const el = e.currentTarget as HTMLButtonElement;
        el.style.transform = "translateY(-1px)";
        el.style.boxShadow = primary ? "0 6px 16px rgba(30,58,95,0.35)" : "0 4px 12px rgba(0,0,0,0.08)";
      }}
      onMouseLeave={(e) => {
        const el = e.currentTarget as HTMLButtonElement;
        el.style.transform = "translateY(0)";
        el.style.boxShadow = primary ? "0 4px 12px rgba(30,58,95,0.25)" : "0 1px 3px rgba(0,0,0,0.04)";
      }}
    >
      <div
        className="flex items-center justify-center rounded-lg"
        style={{
          width: "36px",
          height: "36px",
          backgroundColor: primary ? "rgba(255,255,255,0.15)" : bgColor,
          flexShrink: 0,
        }}
      >
        <span style={{ color: primary ? "white" : color }}>{icon}</span>
      </div>
      <div className="text-left">
        <div style={{ color: primary ? "#FFFFFF" : "#1E3A5F", fontSize: "13.5px", fontWeight: 600, lineHeight: 1.2 }}>
          {label}
        </div>
        <div style={{ color: primary ? "rgba(255,255,255,0.6)" : "#94A3B8", fontSize: "11px", marginTop: "1px" }}>
          {sublabel}
        </div>
      </div>
    </button>
  );
}

const actions: ActionBtnProps[] = [
  {
    icon: <FilePlus size={18} />,
    label: "New Invoice",
    sublabel: "Create & send",
    color: "#3B82F6",
    bgColor: "#EFF6FF",
    borderColor: "#BFDBFE",
    primary: true,
  },
  {
    icon: <Receipt size={18} />,
    label: "Receipt Voucher",
    sublabel: "Record payment",
    color: "#10B981",
    bgColor: "#ECFDF5",
    borderColor: "#A7F3D0",
  },
  {
    icon: <BookOpen size={18} />,
    label: "Journal Entry",
    sublabel: "Manual posting",
    color: "#8B5CF6",
    bgColor: "#F5F3FF",
    borderColor: "#DDD6FE",
  },
  {
    icon: <UserPlus size={18} />,
    label: "Add Customer",
    sublabel: "New contact",
    color: "#F59E0B",
    bgColor: "#FFFBEB",
    borderColor: "#FDE68A",
  },
  {
    icon: <Upload size={18} />,
    label: "Import Data",
    sublabel: "CSV / Excel",
    color: "#06B6D4",
    bgColor: "#ECFEFF",
    borderColor: "#A5F3FC",
  },
  {
    icon: <Download size={18} />,
    label: "Export Report",
    sublabel: "PDF / Excel",
    color: "#64748B",
    bgColor: "#F8FAFC",
    borderColor: "#E2E8F0",
  },
];

export function QuickActions() {
  return (
    <div
      className="p-4 rounded-xl"
      style={{
        backgroundColor: "#FFFFFF",
        boxShadow: "0 1px 4px rgba(0,0,0,0.06), 0 4px 16px rgba(0,0,0,0.04)",
        border: "1px solid #F1F5F9",
        fontFamily: "'Inter', sans-serif",
      }}
    >
      <div className="flex items-center justify-between mb-3">
        <h3 style={{ color: "#0F172A", fontSize: "14px", fontWeight: 700, margin: 0 }}>
          Quick Actions
        </h3>
        <span style={{ color: "#94A3B8", fontSize: "12px" }}>Shortcuts</span>
      </div>
      <div className="flex gap-3">
        {actions.map((action) => (
          <ActionBtn key={action.label} {...action} />
        ))}
      </div>
    </div>
  );
}