import { Bell, Search, ChevronDown, Calendar } from "lucide-react";

interface HeaderProps {
  title: string;
  subtitle: string;
}

export function Header({ title, subtitle }: HeaderProps) {
  const today = new Date();
  const dateStr = today.toLocaleDateString("en-US", {
    weekday: "long",
    year: "numeric",
    month: "long",
    day: "numeric",
  });

  return (
    <div
      className="flex items-center px-6 gap-4"
      style={{
        height: "64px",
        minHeight: "64px",
        backgroundColor: "#FFFFFF",
        borderBottom: "1px solid #E5E9F0",
        fontFamily: "'Inter', sans-serif",
        zIndex: 10,
        boxShadow: "0 1px 4px rgba(0,0,0,0.06)",
      }}
    >
      {/* Page Title */}
      <div className="flex-shrink-0">
        <h1 style={{ color: "#1E3A5F", fontSize: "17px", fontWeight: 700, margin: 0, lineHeight: 1 }}>
          {title}
        </h1>
        <p style={{ color: "#94A3B8", fontSize: "11.5px", margin: 0, marginTop: "2px", fontWeight: 400 }}>
          {subtitle}
        </p>
      </div>

      {/* Date */}
      <div
        className="flex items-center gap-2 px-3 py-1.5 rounded-lg"
        style={{ backgroundColor: "#F1F5F9", marginLeft: "12px" }}
      >
        <Calendar size={13} style={{ color: "#64748B" }} />
        <span style={{ color: "#64748B", fontSize: "12px", fontWeight: 500 }}>{dateStr}</span>
      </div>

      {/* Search Bar */}
      <div className="flex-1 flex justify-center">
        <div
          className="flex items-center gap-3 px-4 py-2 rounded-xl"
          style={{
            backgroundColor: "#F5F7FA",
            border: "1px solid #E2E8F0",
            width: "380px",
            maxWidth: "100%",
          }}
        >
          <Search size={15} style={{ color: "#94A3B8", flexShrink: 0 }} />
          <input
            type="text"
            placeholder="Search invoices, customers, transactions..."
            style={{
              flex: 1,
              border: "none",
              outline: "none",
              background: "transparent",
              color: "#334155",
              fontSize: "13px",
              fontFamily: "'Inter', sans-serif",
            }}
          />
          <kbd
            style={{
              backgroundColor: "#E2E8F0",
              color: "#94A3B8",
              fontSize: "10px",
              padding: "2px 6px",
              borderRadius: "4px",
              fontFamily: "monospace",
              flexShrink: 0,
            }}
          >
            Ctrl+K
          </kbd>
        </div>
      </div>

      {/* Right Actions */}
      <div className="flex items-center gap-3 flex-shrink-0">
        {/* Notification Bell */}
        <button
          className="relative flex items-center justify-center rounded-xl"
          style={{
            width: "38px",
            height: "38px",
            backgroundColor: "#F5F7FA",
            border: "1px solid #E2E8F0",
            cursor: "pointer",
          }}
        >
          <Bell size={17} style={{ color: "#64748B" }} />
          <span
            className="absolute flex items-center justify-center rounded-full"
            style={{
              top: "6px",
              right: "6px",
              width: "8px",
              height: "8px",
              backgroundColor: "#EF4444",
              border: "2px solid white",
            }}
          />
        </button>

        {/* Fiscal Year Selector */}
        <button
          className="flex items-center gap-2 px-3 py-2 rounded-xl"
          style={{
            backgroundColor: "#EFF6FF",
            border: "1px solid #BFDBFE",
            cursor: "pointer",
          }}
        >
          <span style={{ color: "#1D4ED8", fontSize: "12.5px", fontWeight: 600 }}>FY 2025-26</span>
          <ChevronDown size={13} style={{ color: "#3B82F6" }} />
        </button>

        {/* User Avatar */}
        <div className="flex items-center gap-2 cursor-pointer">
          <div
            className="flex items-center justify-center rounded-full"
            style={{
              width: "36px",
              height: "36px",
              background: "linear-gradient(135deg, #3B82F6, #8B5CF6)",
            }}
          >
            <span style={{ color: "white", fontSize: "12px", fontWeight: 700 }}>AH</span>
          </div>
          <ChevronDown size={13} style={{ color: "#94A3B8" }} />
        </div>
      </div>
    </div>
  );
}