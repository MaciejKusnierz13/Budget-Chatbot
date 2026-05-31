
const state = { profileName: 'Jan Kowalski', avatarDataUrl: null, style: 'dark-gray' };

document.querySelectorAll('.style-dot').forEach(dot => {
  dot.addEventListener('click', () => {
    const s = dot.dataset.s;
    document.body.setAttribute('data-style', s);
    state.style = s;
    document.querySelectorAll('.style-dot').forEach(d => d.classList.toggle('active', d.dataset.s === s));
  });
});

const profileTrigger = document.getElementById('profileTrigger');
const profilePopup = document.getElementById('profilePopup');
const overlay = document.getElementById('overlay');

profileTrigger.addEventListener('click', e => {
  e.stopPropagation();
  const open = profilePopup.classList.contains('open');
  profilePopup.classList.toggle('open', !open);
  overlay.classList.toggle('open', !open);
});
overlay.addEventListener('click', () => {
  profilePopup.classList.remove('open');
  overlay.classList.remove('open');
});
document.addEventListener('keydown', e => {
  if (e.key === 'Escape') {
    profilePopup.classList.remove('open');
    overlay.classList.remove('open');
  }
});

const avatarInput = document.getElementById('avatar-file-input');
document.getElementById('popupAvatarPreview').addEventListener('click', () => avatarInput.click());
avatarInput.addEventListener('change', e => {
  const file = e.target.files[0];
  if (!file) return;
  const reader = new FileReader();
  reader.onload = ev => { state.avatarDataUrl = ev.target.result; applyAvatar(); };
  reader.readAsDataURL(file);
});
function applyAvatar() {
  const imgs = ['topAvatarImg', 'popupAvatarImg', 'userMsgAvatarImg'];
  const inits = ['topAvatarInitials', 'popupAvatarInitials', 'userMsgAvatarInitials'];
  if (state.avatarDataUrl) {
    imgs.forEach(id => { const el = document.getElementById(id); if (el) { el.src = state.avatarDataUrl; el.style.display = 'block'; } });
    inits.forEach(id => { const el = document.getElementById(id); if (el) el.style.display = 'none'; });
  } else {
    imgs.forEach(id => { const el = document.getElementById(id); if (el) el.style.display = 'none'; });
    inits.forEach(id => { const el = document.getElementById(id); if (el) el.style.display = ''; });
  }
}

document.getElementById('profileSaveBtn').addEventListener('click', () => {
  const name = document.getElementById('profileNameInput').value.trim() || 'Użytkownik';
  state.profileName = name;
  const initials = name.split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase();
  ['topAvatarInitials', 'popupAvatarInitials', 'userMsgAvatarInitials'].forEach(id => {
    const el = document.getElementById(id); if (el) el.textContent = initials;
  });
  document.getElementById('userMsgMeta').textContent = name + ' · 14:32';
  profilePopup.classList.remove('open');
  overlay.classList.remove('open');
  const t = document.getElementById('toast');
  t.classList.add('show');
  setTimeout(() => t.classList.remove('show'), 2000);
});

document.getElementById('logoMark').addEventListener('click', () => {
  document.getElementById('topbarTitle').textContent = 'Jak zaplanować podróż do Japonii?';
  document.querySelectorAll('.chat-item').forEach(i => i.classList.toggle('active', i.dataset.chatId === '1'));
});

document.querySelectorAll('.chat-item').forEach(item => {
  item.addEventListener('click', () => {
    if (item.dataset.chatId === '1') {
      document.querySelectorAll('.chat-item').forEach(i => i.classList.remove('active'));
      item.classList.add('active');
      document.getElementById('topbarTitle').textContent = 'Jak zaplanować podróż do Japonii?';
    }
  });
});

const textarea = document.getElementById('messageInput');
textarea.addEventListener('input', () => {
  textarea.style.height = 'auto';
  textarea.style.height = Math.min(textarea.scrollHeight, 140) + 'px';
});
textarea.addEventListener('keydown', e => {
  if (e.key === 'Enter' && !e.shiftKey) e.preventDefault();
});