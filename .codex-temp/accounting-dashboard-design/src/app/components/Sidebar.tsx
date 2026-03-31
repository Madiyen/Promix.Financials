import React from "react";
import {
  LayoutDashboard,
  FileText,
  Users,
  Building2,
  Package,
  CreditCard,
  BarChart3,
  Settings,
  ChevronRight,
  TrendingUp,
  Receipt,
  BookOpen,
} from "lucide-react";

interface NavItem {
  icon: React.ReactNode;
  label: string;
  badge?: number;
}

const navItems: NavItem[] = [
  { icon: <LayoutDashboard size={18} />, label: "Dashboard" },
  { icon: <FileText size={18} />, label: "Invoices", badge: 12 },
  { icon: <Users size={18} />, label: "Customers" },
  { icon: <Building2 size={18} />, label: "Vendors" },
  { icon: <Package size={18} />, label: "Inventory" },
  { icon: <CreditCard size={18} />, label: "Accounts" },
  { icon: <Receipt size={18} />, label: "Vouchers", badge: 3 },
  { icon: <BookOpen size={18} />, label: "Journal", badge: 2 },
  { icon: <BarChart3 size={18} />, label: "Reports" },
  { icon: <Settings size={18} />, label: "Settings" },
];

interface SidebarProps {
  activePage: string;
  onNavigate: (page: string) => void;
}

export function Sidebar({ activePage, onNavigate }: SidebarProps) {
  return (
    <div
      className="flex flex-col h-full"
      style={{
        width: "280px",
        minWidth: "280px",
        backgroundColor: "#1E3A5F",
        fontFamily: "'Inter', sans-serif",
      }}
    >
      {/* Logo */}
      <div
        className="flex items-center gap-3 px-6 py-5"
        style={{ borderBottom: "1px solid rgba(255,255,255,0.08)" }}
      >
        <div
          className="flex items-center justify-center rounded-xl"
          style={{
            width: "40px",
            height: "40px",
            background: "linear-gradient(135deg, #3B82F6, #1D4ED8)",
            boxShadow: "0 4px 12px rgba(59,130,246,0.4)",
          }}
        >
          <TrendingUp size={20} color="white" />
        </div>
        <div>
          <div
            style={{
              color: "#FFFFFF",
              fontSize: "15px",
              fontWeight: 700,
              letterSpacing: "0.3px",
            }}
          >
            AccoFlow
          </div>
          <div style={{ color: "rgba(255,255,255,0.45)", fontSize: "11px", fontWeight: 400 }}>
            Pro Accounting Suite
          </div>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 px-4 py-5 overflow-y-auto">
        <div style={{ color: "rgba(255,255,255,0.35)", fontSize: "10px", fontWeight: 600, letterSpacing: "1.2px", textTransform: "uppercase", marginBottom: "12px", paddingLeft: "8px" }}>
          Main Menu
        </div>
        <ul className="flex flex-col gap-1">
          {navItems.map((item) => {
            const isActive = activePage === item.label;
            return (
              <li key={item.label}>
                <button
                  onClick={() => onNavigate(item.label)}
                  className="w-full flex items-center gap-3 px-3 py-2.5 rounded-xl transition-all duration-150 group relative"
                  style={{
                    backgroundColor: isActive ? "rgba(59,130,246,0.2)" : "transparent",
                    border: isActive ? "1px solid rgba(59,130,246,0.3)" : "1px solid transparent",
                    cursor: "pointer",
                  }}
                  onMouseEnter={(e) => {
                    if (!isActive) {
                      (e.currentTarget as HTMLButtonElement).style.backgroundColor = "rgba(255,255,255,0.06)";
                    }
                  }}
                  onMouseLeave={(e) => {
                    if (!isActive) {
                      (e.currentTarget as HTMLButtonElement).style.backgroundColor = "transparent";
                    }
                  }}
                >
                  {isActive && (
                    <div
                      style={{
                        position: "absolute",
                        left: 0,
                        top: "50%",
                        transform: "translateY(-50%)",
                        width: "3px",
                        height: "20px",
                        backgroundColor: "#3B82F6",
                        borderRadius: "0 3px 3px 0",
                      }}
                    />
                  )}
                  <span style={{ color: isActive ? "#3B82F6" : "rgba(255,255,255,0.6)" }}>
                    {item.icon}
                  </span>
                  <span
                    className="flex-1 text-left"
                    style={{
                      color: isActive ? "#FFFFFF" : "rgba(255,255,255,0.7)",
                      fontSize: "13.5px",
                      fontWeight: isActive ? 600 : 400,
                    }}
                  >
                    {item.label}
                  </span>
                  {item.badge && (
                    <span
                      className="flex items-center justify-center rounded-full"
                      style={{
                        backgroundColor: "#EF4444",
                        color: "#FFFFFF",
                        fontSize: "10px",
                        fontWeight: 700,
                        minWidth: "18px",
                        height: "18px",
                        padding: "0 5px",
                      }}
                    >
                      {item.badge}
                    </span>
                  )}
                  {!item.badge && isActive && (
                    <ChevronRight size={14} style={{ color: "rgba(255,255,255,0.4)" }} />
                  )}
                </button>
              </li>
            );
          })}
        </ul>

        {/* Quick Stats */}
        <div
          className="mt-6 rounded-xl p-4"
          style={{ backgroundColor: "rgba(59,130,246,0.12)", border: "1px solid rgba(59,130,246,0.2)" }}
        >
          <div style={{ color: "rgba(255,255,255,0.5)", fontSize: "10px", fontWeight: 600, letterSpacing: "0.8px", textTransform: "uppercase", marginBottom: "10px" }}>
            This Month
          </div>
          <div className="flex flex-col gap-2">
            <div className="flex justify-between items-center">
              <span style={{ color: "rgba(255,255,255,0.6)", fontSize: "12px" }}>Revenue</span>
              <span style={{ color: "#10B981", fontSize: "12px", fontWeight: 600 }}>$48,290</span>
            </div>
            <div className="flex justify-between items-center">
              <span style={{ color: "rgba(255,255,255,0.6)", fontSize: "12px" }}>Expenses</span>
              <span style={{ color: "#F59E0B", fontSize: "12px", fontWeight: 600 }}>$12,840</span>
            </div>
            <div style={{ height: "1px", backgroundColor: "rgba(255,255,255,0.1)", margin: "4px 0" }} />
            <div className="flex justify-between items-center">
              <span style={{ color: "rgba(255,255,255,0.6)", fontSize: "12px" }}>Net Profit</span>
              <span style={{ color: "#FFFFFF", fontSize: "13px", fontWeight: 700 }}>$35,450</span>
            </div>
          </div>
          {/* Progress bar */}
          <div style={{ marginTop: "10px" }}>
            <div style={{ height: "4px", backgroundColor: "rgba(255,255,255,0.1)", borderRadius: "2px" }}>
              <div style={{ height: "4px", width: "73%", backgroundColor: "#3B82F6", borderRadius: "2px" }} />
            </div>
            <div style={{ color: "rgba(255,255,255,0.4)", fontSize: "10px", marginTop: "4px" }}>73% of monthly target</div>
          </div>
        </div>
      </nav>

      {/* User Profile */}
      <div
        className="px-4 py-4"
        style={{ borderTop: "1px solid rgba(255,255,255,0.08)" }}
      >
        <div
          className="flex items-center gap-3 p-3 rounded-xl cursor-pointer"
          style={{ backgroundColor: "rgba(255,255,255,0.06)" }}
        >
          <div
            className="flex items-center justify-center rounded-full"
            style={{
              width: "36px",
              height: "36px",
              background: "linear-gradient(135deg, #3B82F6, #8B5CF6)",
              flexShrink: 0,
            }}
          >
            <span style={{ color: "white", fontSize: "13px", fontWeight: 700 }}>AH</span>
          </div>
          <div className="flex-1 min-w-0">
            <div style={{ color: "#FFFFFF", fontSize: "13px", fontWeight: 600, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>
              Ahmed Hassan
            </div>
            <div style={{ color: "rgba(255,255,255,0.45)", fontSize: "11px", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>
              Senior Accountant
            </div>
          </div>
          <div
            style={{
              width: "8px",
              height: "8px",
              borderRadius: "50%",
              backgroundColor: "#10B981",
              flexShrink: 0,
            }}
          />
        </div>
      </div>
    </div>
  );
}