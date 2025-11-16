// Front-end for the plugin.
// Uses Jellyfin's ApiClient if available; otherwise falls back to fetch.
(function(){
  const ApiClient = window.ApiClient || null;

  function apiGet(path){
    if (ApiClient && ApiClient.getJSON) return ApiClient.getJSON(path);
    return fetch(path, {credentials:'same-origin'}).then(r=>r.json());
  }
  function apiPost(path, body){
    if (ApiClient && ApiClient.ajax) return ApiClient.ajax({type:'POST', url:path, data: body});
    return fetch(path, {method:'POST', credentials:'same-origin'});
  }

  // Discover current user id if possible
  const currentUserId = (window.ApiClient && ApiClient._auth && ApiClient._auth.currentUserId) || null;

  // UI elements
  const list = document.getElementById('history-list');
  const userTabs = document.getElementById('user-tabs');
  const filterInput = document.getElementById('global-filter');
  const typeFilter = document.getElementById('type-filter');
  const sortBy = document.getElementById('sort-by');
  const sortOrder = document.getElementById('sort-order');

  let users = []; // list of users (id,name)
  let activeUserId = currentUserId;
  let cache = [];

  async function fetchUsers(){
    // Try to use Jellyfin API to get users when ApiClient is present
    if (ApiClient && ApiClient.getJSON){
      try{
        const all = await ApiClient.getJSON('/Users');
        users = all.map(u=>({Id:u.Id, Name:u.Name}));
      } catch(e){ users = []; }
    }
    // fallback: only current user
    if (!users.length && currentUserId){
      users = [{Id: currentUserId, Name: 'Me'}];
    }
    if (!users.length){
      users = [{Id: '', Name: 'All Users'}];
    }
    renderTabs();
  }

  function renderTabs(){
    userTabs.innerHTML = '';
    users.forEach(u=>{
      const t = document.createElement('div');
      t.className = 'jf-tab' + (u.Id===activeUserId ? ' active' : '');
      t.textContent = u.Name || 'Unknown';
      t.onclick = ()=>{ activeUserId = u.Id; loadHistory(); renderTabs(); };
      userTabs.appendChild(t);
    });
  }

  async function loadHistory(){
    list.innerHTML = '<div>Loading...</div>';
    const q = new URLSearchParams();
    if (activeUserId) q.set('userId', activeUserId);
    if (filterInput.value) q.set('filter', filterInput.value);
    if (typeFilter.value) q.set('type', typeFilter.value);
    q.set('sort', sortBy.value);
    q.set('order', sortOrder.value);

    const url = '/WatchHistoryRating/History?' + q.toString();
    try{
      const items = await apiGet(url);
      cache = items;
      renderList(items);
    }catch(e){
      list.innerHTML = '<div>Error loading history</div>';
    }
  }

  function renderList(items){
    list.innerHTML = '';
    if (!items || !items.length) { list.innerHTML = '<div>No items found</div>'; return; }
    items.forEach(it=>{
      const card = document.createElement('div'); card.className = 'history-card';
      const img = document.createElement('img');
      // Use Jellyfin scaled image path if available
      if (ApiClient && ApiClient.getImageUrl) img.src = ApiClient.getImageUrl(it.Id, 'Primary', 0, { maxWidth: 200 });
      else img.src = it.Image || '';

      const meta = document.createElement('div'); meta.className='item-meta';
      const title = document.createElement('h3'); title.className='item-title'; title.textContent = it.Name;
      const sub = document.createElement('div'); sub.className='item-sub'; sub.textContent = `${it.Type} • Last played: ${it.LastPlayed ? new Date(it.LastPlayed).toLocaleString() : '—'}`;

      const ratingRow = document.createElement('div'); ratingRow.className='rating-row';
      const input = document.createElement('input'); input.className='rating-input'; input.type='number'; input.min=1; input.max=10;
      input.value = it.UserRating || '';
      input.id = 'rate-'+it.Id;

      const save = document.createElement('button'); save.className='save-btn'; save.textContent='Save';
      save.onclick = ()=> saveRating(it.Id);

      ratingRow.appendChild(input); ratingRow.appendChild(save);

      meta.appendChild(title); meta.appendChild(sub); meta.appendChild(ratingRow);

      card.appendChild(img); card.appendChild(meta);
      list.appendChild(card);
    });
  }

  async function saveRating(itemId){
    const val = document.getElementById('rate-'+itemId).value;
    const rating = parseInt(val);
    if (!rating || rating < 1 || rating > 10){ alert('Please enter a rating 1-10'); return; }
    const userId = activeUserId || currentUserId || '';
    const q = '/WatchHistoryRating/Rate?userId='+encodeURIComponent(userId)+'&itemId='+encodeURIComponent(itemId)+'&rating='+rating;
    try{
      await apiPost(q);
      alert('Saved');
      loadHistory();
    }catch(e){ alert('Error saving rating'); }
  }

  // wire controls
  filterInput.addEventListener('input', ()=> { setTimeout(loadHistory, 250); });
  typeFilter.addEventListener('change', loadHistory);
  sortBy.addEventListener('change', loadHistory);
  sortOrder.addEventListener('change', loadHistory);

  // init
  fetchUsers().then(loadHistory);
})();
