import { useEffect, useState, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';

interface ChatChannel {
  id: string;
  key: string;
  name: string;
  type: string;
  isSystem: boolean;
}

interface ChatMessage {
  id: string;
  channelKey: string;
  userId: string;
  userName?: string;
  body: string;
  createdUtc: string;
}

export default function Chat() {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const [channels, setChannels] = useState<ChatChannel[]>([]);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [activeChannel, setActiveChannel] = useState<string>('general');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [newMessage, setNewMessage] = useState('');
  const [sending, setSending] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    loadChannels();
  }, []);

  useEffect(() => {
    if (activeChannel) {
      loadMessages(activeChannel);
      // Poll for new messages every 3 seconds
      const interval = setInterval(() => loadMessages(activeChannel), 3000);
      return () => clearInterval(interval);
    }
  }, [activeChannel]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  async function loadChannels() {
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) throw new Error('Authentication required');

      const resp = await fetch('/api/v1/chat/channels', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      if (!resp.ok) throw new Error('Failed to load channels');
      const data = await resp.json();
      setChannels(data.data || []);
      setLoading(false);
    } catch (e: any) {
      setError(e.message);
      setLoading(false);
    }
  }

  async function loadMessages(channelKey: string) {
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) return;

      const resp = await fetch(`/api/v1/chat/${channelKey}/messages?page=1&pageSize=100`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      if (!resp.ok) throw new Error('Failed to load messages');
      const data = await resp.json();
      setMessages(data.data || []);
    } catch (e: any) {
      console.error('Error loading messages:', e);
    }
  }

  async function sendMessage() {
    if (!newMessage.trim()) return;
    
    setSending(true);
    setError(null);
    
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) throw new Error('Authentication required');
      
      const resp = await fetch(`/api/v1/chat/${activeChannel}/messages`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          body: newMessage
        })
      });

      if (!resp.ok) throw new Error('Failed to send message');
      
      setNewMessage('');
      loadMessages(activeChannel);
      
    } catch (e: any) {
      setError(e.message);
    } finally {
      setSending(false);
    }
  }

  function formatMessageTime(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffMins < 1440) return `${Math.floor(diffMins / 60)}h ago`;
    
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  function handleKeyPress(e: React.KeyboardEvent) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  }

  if (loading) {
    return (
      <div style={styles.container}>
        <div style={styles.loading}>Loading chat...</div>
      </div>
    );
  }

  return (
    <div style={styles.container}>
      <header style={styles.header}>
        <h1 style={styles.title}>Chat</h1>
        <button onClick={() => navigate(`/tenant/${slug}`)} style={styles.backButton}>
          ← Back to Dashboard
        </button>
      </header>

      {error && <div style={styles.error}>{error}</div>}

      <div style={styles.chatLayout}>
        {/* Channels Sidebar */}
        <aside style={styles.channelsSidebar}>
          <h2 style={styles.sidebarTitle}>Channels</h2>
          <div style={styles.channelsList}>
            {channels.map(channel => (
              <button
                key={channel.id}
                onClick={() => setActiveChannel(channel.key)}
                style={{
                  ...styles.channelButton,
                  background: activeChannel === channel.key ? '#28a745' : '#f8f9fa',
                  color: activeChannel === channel.key ? '#fff' : '#333'
                }}
              >
                # {channel.name}
                {channel.isSystem && (
                  <span style={styles.systemBadge}>System</span>
                )}
              </button>
            ))}
          </div>
        </aside>

        {/* Messages Area */}
        <main style={styles.messagesArea}>
          <div style={styles.messagesHeader}>
            <h2 style={styles.channelName}>
              # {channels.find(c => c.key === activeChannel)?.name || activeChannel}
            </h2>
          </div>

          <div style={styles.messagesContainer}>
            {messages.length === 0 ? (
              <div style={styles.emptyState}>
                No messages yet. Start the conversation!
              </div>
            ) : (
              messages.map(message => (
                <div key={message.id} style={styles.message}>
                  <div style={styles.messageHeader}>
                    <span style={styles.userName}>
                      {message.userName || 'Anonymous'}
                    </span>
                    <span style={styles.messageTime}>
                      {formatMessageTime(message.createdUtc)}
                    </span>
                  </div>
                  <div style={styles.messageBody}>{message.body}</div>
                </div>
              ))
            )}
            <div ref={messagesEndRef} />
          </div>

          <div style={styles.messageInput}>
            <textarea
              value={newMessage}
              onChange={(e) => setNewMessage(e.target.value)}
              onKeyPress={handleKeyPress}
              placeholder={`Message #${channels.find(c => c.key === activeChannel)?.name || activeChannel}`}
              style={styles.textarea}
              rows={2}
              disabled={sending}
            />
            <button
              onClick={sendMessage}
              disabled={sending || !newMessage.trim()}
              style={styles.sendButton}
            >
              {sending ? 'Sending...' : 'Send'}
            </button>
          </div>
        </main>
      </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    padding: '1rem',
    maxWidth: '1400px',
    margin: '0 auto',
    height: 'calc(100vh - 120px)'
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: '1rem'
  },
  title: {
    margin: '0',
    fontSize: '2rem',
    fontWeight: '700',
    color: '#333'
  },
  backButton: {
    background: '#f8f9fa',
    border: '1px solid #ddd',
    borderRadius: '8px',
    padding: '0.5rem 1rem',
    cursor: 'pointer',
    fontSize: '0.9rem',
    color: '#666'
  },
  loading: {
    textAlign: 'center',
    padding: '3rem',
    fontSize: '1.1rem',
    color: '#666'
  },
  error: {
    background: '#f8d7da',
    color: '#721c24',
    padding: '1rem',
    borderRadius: '8px',
    marginBottom: '1rem'
  },
  chatLayout: {
    display: 'flex',
    gap: '1rem',
    height: 'calc(100% - 80px)',
    overflow: 'hidden'
  },
  channelsSidebar: {
    width: '250px',
    background: '#fff',
    borderRadius: '12px',
    padding: '1rem',
    boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
    overflowY: 'auto'
  },
  sidebarTitle: {
    margin: '0 0 1rem 0',
    fontSize: '1.1rem',
    fontWeight: '600',
    color: '#333'
  },
  channelsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: '0.5rem'
  },
  channelButton: {
    border: 'none',
    borderRadius: '6px',
    padding: '0.75rem',
    textAlign: 'left',
    cursor: 'pointer',
    fontSize: '0.95rem',
    fontWeight: '500',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    transition: 'all 0.2s'
  },
  systemBadge: {
    fontSize: '0.7rem',
    padding: '2px 6px',
    background: 'rgba(255,255,255,0.3)',
    borderRadius: '4px'
  },
  messagesArea: {
    flex: 1,
    background: '#fff',
    borderRadius: '12px',
    boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden'
  },
  messagesHeader: {
    padding: '1rem 1.5rem',
    borderBottom: '1px solid #e9ecef'
  },
  channelName: {
    margin: 0,
    fontSize: '1.25rem',
    fontWeight: '600',
    color: '#333'
  },
  messagesContainer: {
    flex: 1,
    padding: '1rem 1.5rem',
    overflowY: 'auto',
    display: 'flex',
    flexDirection: 'column',
    gap: '1rem'
  },
  emptyState: {
    textAlign: 'center',
    padding: '3rem',
    color: '#666',
    fontSize: '1rem'
  },
  message: {
    display: 'flex',
    flexDirection: 'column',
    gap: '0.25rem'
  },
  messageHeader: {
    display: 'flex',
    alignItems: 'baseline',
    gap: '0.5rem'
  },
  userName: {
    fontWeight: '600',
    color: '#333',
    fontSize: '0.95rem'
  },
  messageTime: {
    fontSize: '0.8rem',
    color: '#999'
  },
  messageBody: {
    fontSize: '0.95rem',
    color: '#555',
    lineHeight: '1.5',
    whiteSpace: 'pre-wrap'
  },
  messageInput: {
    padding: '1rem 1.5rem',
    borderTop: '1px solid #e9ecef',
    display: 'flex',
    gap: '0.5rem',
    alignItems: 'flex-end'
  },
  textarea: {
    flex: 1,
    padding: '0.75rem',
    border: '1px solid #ddd',
    borderRadius: '6px',
    fontSize: '1rem',
    resize: 'none',
    fontFamily: 'inherit'
  },
  sendButton: {
    background: '#28a745',
    color: '#fff',
    border: 'none',
    borderRadius: '6px',
    padding: '0.75rem 1.5rem',
    fontSize: '1rem',
    fontWeight: '500',
    cursor: 'pointer',
    alignSelf: 'stretch'
  }
};
