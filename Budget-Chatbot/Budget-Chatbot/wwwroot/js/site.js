var API_URL = '/api/chat';

var state = {
    chats: [],
    activeChatId: null,
    sending: false
};

function nowTime() {
    return new Date().toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit' });
}

function escHtml(str) {
    if (!str) return '';
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function showToast(msg, isError) {
    var t = document.getElementById('toast');
    t.textContent = msg;
    t.classList.toggle('error', !!isError);
    t.classList.add('show');
    setTimeout(function () { t.classList.remove('show'); }, 2500);
}

document.querySelectorAll('.style-dot').forEach(function (dot) {
    dot.addEventListener('click', function () {
        var s = dot.dataset.s;
        document.body.setAttribute('data-style', s);
        document.querySelectorAll('.style-dot').forEach(function (d) {
            d.classList.toggle('active', d.dataset.s === s);
        });
    });
});

var helpPopup = document.getElementById('helpPopup');
var infoBtn = document.getElementById('infoBtn');
var overlay = document.getElementById('overlay');

infoBtn.addEventListener('mouseenter', function () {
    helpPopup.classList.add('open');
});

infoBtn.addEventListener('mouseleave', function (e) {
    if (!helpPopup.contains(e.relatedTarget)) {
        helpPopup.classList.remove('open');
    }
});

helpPopup.addEventListener('mouseleave', function () {
    helpPopup.classList.remove('open');
});

overlay.addEventListener('click', function () {
    helpPopup.classList.remove('open');
    overlay.classList.remove('open');
});

function generateSessionId() {
    return 'sess_' + Date.now() + '_' + Math.random().toString(36).slice(2, 8);
}

function createNewChat() {
    var id = generateSessionId();
    var chat = { id: id, title: 'Nowy chat', messages: [] };
    state.chats.unshift(chat);
    state.activeChatId = id;
    renderSidebar();
    renderMessages();
    document.getElementById('topbarTitle').textContent = chat.title;
    document.getElementById('messageInput').focus();
}

async function deleteChat(id) {
    try {
        await fetch('/api/chat/history/' + encodeURIComponent(id), { method: 'DELETE' });
    } catch (e) { }

    state.chats = state.chats.filter(function (c) { return c.id !== id; });

    if (state.activeChatId === id) {
        if (state.chats.length > 0) {
            state.activeChatId = state.chats[0].id;
            document.getElementById('topbarTitle').textContent = state.chats[0].title;
        } else {
            createNewChat();
            return;
        }
    }

    renderSidebar();
    renderMessages();
}

function renderSidebar() {
    var list = document.getElementById('chatList');
    list.innerHTML = '';

    if (state.chats.length === 0) {
        list.innerHTML = '<div style="padding:8px 14px;font-size:11px;color:#555">Brak historii</div>';
        return;
    }

    state.chats.forEach(function (chat) {
        var item = document.createElement('div');
        item.className = 'chat-item' + (chat.id === state.activeChatId ? ' active' : '');
        item.innerHTML =
            '<div class="chat-item-title">' + escHtml(chat.title) + '</div>' +
            '<button class="chat-delete-btn" title="Usuń">✕</button>';

        item.querySelector('.chat-item-title').addEventListener('click', function () {
            state.activeChatId = chat.id;
            document.getElementById('topbarTitle').textContent = chat.title;
            renderSidebar();
            renderMessages();
        });

        item.querySelector('.chat-delete-btn').addEventListener('click', function (e) {
            e.stopPropagation();
            deleteChat(chat.id);
        });

        list.appendChild(item);
    });
}

function activeChat() {
    return state.chats.find(function (c) { return c.id === state.activeChatId; }) || null;
}

function renderMessages() {
    var wrapper = document.getElementById('messagesWrapper');
    var chat = activeChat();

    if (!chat || chat.messages.length === 0) {
        wrapper.innerHTML =
            '<div class="empty-state">' +
            '<p>Wpisz wydatek, np. <em>Kawa 12 zł</em></p>' +
            '</div>';
        return;
    }

    var html = '';

    chat.messages.forEach(function (msg) {
        if (msg.role === 'user') {
            html +=
                '<div class="msg-row user">' +
                '<div class="msg-avatar"><span>' + escHtml(APP_USER_INITIAL) + '</span></div>' +
                '<div>' +
                '<div class="msg-bubble">' + escHtml(msg.text) + '</div>' +
                '<div class="msg-meta">' + escHtml(APP_USERNAME) + ' · ' + msg.time + '</div>' +
                '</div></div>';
        } else {
            var cardHtml = '';
            if (msg.transaction) {
                var t = msg.transaction;
                cardHtml =
                    '<div class="transaction-card">' +
                    '<div class="tc-row"><span class="tc-label">Kwota</span><span class="tc-value tc-amount">' + t.amount + ' zł</span></div>' +
                    '<div class="tc-row"><span class="tc-label">Kategoria</span><span class="tc-value">' + escHtml(t.categoryName || 'ID: ' + t.categoryId) + '</span></div>' +
                    '<div class="tc-row"><span class="tc-label">Opis</span><span class="tc-value">' + escHtml(t.description || '—') + '</span></div>' +
                    '</div>';
            }
            html +=
                '<div class="msg-row ai">' +
                '<div class="msg-avatar ai-avatar">BC</div>' +
                '<div>' +
                '<div class="msg-bubble">' + escHtml(msg.text) + cardHtml + '</div>' +
                '<div class="msg-meta">Budget Chatbot · ' + msg.time + '</div>' +
                '</div></div>';
        }
    });

    wrapper.innerHTML = html;
    document.getElementById('chatArea').scrollTop = 999999;
}

function addTypingIndicator() {
    var wrapper = document.getElementById('messagesWrapper');
    var existing = document.getElementById('typingIndicator');
    if (existing) existing.remove();
    var el = document.createElement('div');
    el.className = 'msg-row ai';
    el.id = 'typingIndicator';
    el.innerHTML =
        '<div class="msg-avatar ai-avatar">BC</div>' +
        '<div><div class="msg-bubble">' +
        '<div class="typing-dots"><span></span><span></span><span></span></div>' +
        '</div></div>';
    wrapper.appendChild(el);
    document.getElementById('chatArea').scrollTop = 999999;
}

function removeTypingIndicator() {
    var el = document.getElementById('typingIndicator');
    if (el) el.remove();
}

async function saveChatMessage(sessionId, chatTitle, role, content) {
    try {
        await fetch('/api/chat/history', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                sessionId: sessionId,
                chatTitle: chatTitle,
                role: role,
                content: content
            })
        });
    } catch (e) { }
}

async function sendMessage() {
    if (state.sending) return;
    var input = document.getElementById('messageInput');
    var text = input.value.trim();
    if (!text) return;

    if (!state.activeChatId) createNewChat();
    var chat = activeChat();
    if (!chat) return;

    if (chat.messages.length === 0) {
        chat.title = text.length > 30 ? text.slice(0, 30) + '…' : text;
        document.getElementById('topbarTitle').textContent = chat.title;
        renderSidebar();
    }

    chat.messages.push({ role: 'user', text: text, time: nowTime() });
    input.value = '';
    input.style.height = 'auto';
    renderMessages();

    await saveChatMessage(chat.id, chat.title, 'User', text);

    state.sending = true;
    document.getElementById('sendBtn').disabled = true;
    addTypingIndicator();

    try {
        var response = await fetch(API_URL + '?message=' + encodeURIComponent(text), { method: 'POST' });
        removeTypingIndicator();

        if (response.ok) {
            var data = await response.json();
            var t = data.savedTransaction;
            var botText = '✅ Zapisano transakcję!';
            var botContent = botText + ' | kwota:' + t.amount + ' kategoria:' + (t.categoryId) + ' opis:' + (t.description || '');

            chat.messages.push({
                role: 'bot',
                text: botText,
                time: nowTime(),
                transaction: {
                    amount: t.amount,
                    categoryId: t.categoryId,
                    categoryName: t.categoryName || null,
                    description: t.description
                }
            });

            await saveChatMessage(chat.id, chat.title, 'Bot', botContent);
            showToast('Zapisano');
        } else {
            var errText = await response.text();
            var botMsg = errText || 'Nie udało się zapisać. Spróbuj dokładniej.';
            chat.messages.push({ role: 'bot', text: '❌ ' + botMsg, time: nowTime() });
            await saveChatMessage(chat.id, chat.title, 'Bot', '❌ ' + botMsg);
            showToast('Błąd', true);
        }
    } catch (err) {
        removeTypingIndicator();
        chat.messages.push({ role: 'bot', text: '⚠️ Brak połączenia z serwerem.', time: nowTime() });
        showToast('Brak połączenia', true);
    }

    state.sending = false;
    document.getElementById('sendBtn').disabled = false;
    renderMessages();
}

async function loadChatsFromDb() {
    try {
        var response = await fetch('/api/chat/history');
        if (!response.ok) return;

        var chats = await response.json();

        chats.forEach(function (c) {
            var messages = c.messages.map(function (m) {
                var isBot = m.role === 'Bot';
                var text = m.content;
                var transaction = null;

                if (isBot && m.content.includes('| kwota:')) {
                    var parts = m.content.split(' | ');
                    text = parts[0];
                    var meta = parts[1] || '';
                    var kwota = parseFloat((meta.match(/kwota:([\d.]+)/) || [])[1]) || 0;
                    var kat = parseInt((meta.match(/kategoria:(\d+)/) || [])[1]) || 0;
                    var opis = (meta.match(/opis:(.*)/) || [])[1] || '';
                    if (kwota > 0) {
                        transaction = { amount: kwota, categoryId: kat, description: opis };
                    }
                }

                return {
                    role: isBot ? 'bot' : 'user',
                    text: text,
                    time: new Date(m.timestamp).toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit' }),
                    transaction: transaction
                };
            });

            state.chats.push({ id: c.sessionId, title: c.title, messages: messages });
        });

        if (state.chats.length > 0) {
            state.activeChatId = state.chats[0].id;
            document.getElementById('topbarTitle').textContent = state.chats[0].title;
            renderSidebar();
            renderMessages();
        } else {
            createNewChat();
        }
    } catch (e) {
        createNewChat();
    }
}

var textarea = document.getElementById('messageInput');

textarea.addEventListener('input', function () {
    textarea.style.height = 'auto';
    textarea.style.height = Math.min(textarea.scrollHeight, 140) + 'px';
});

textarea.addEventListener('keydown', function (e) {
    if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
    }
});

document.getElementById('sendBtn').addEventListener('click', sendMessage);
document.getElementById('newChatBtn').addEventListener('click', createNewChat);

var btnCharts = document.getElementById('btnCharts');
if (btnCharts) {
    btnCharts.addEventListener('click', function () {
        window.location.href = '/Home/Charts';
    });
}

loadChatsFromDb();