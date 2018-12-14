﻿component.data = function () {
    return {
        news: [],
        slides: [],
        tokenTable: [],
        tokenTableSource: [],
        searchText: '',
        control: {
            tab: 'eos'
        },
        sortControl: {
            desc: 0,
            row: null
        }
    };
};

component.created = function () {
    var self = this;
    qv.createView(`/api/v1/lang/${app.lang}/news`, {}, 60000)
        .fetch(x => {
            self.news = x.data;
        });
    qv.createView(`/api/v1/lang/${app.lang}/slides`, {}, 60000)
        .fetch(x => {
            self.slides = x.data;
        });
    qv.get(`/api/v1/lang/${app.lang}/token`, {}).then(res => {
        if (res.code - 0 === 200) {
            self.tokenTableSource = res.data;
            self.tokenTable = res.data;
        }
    })
};

component.methods = {
    searchToken: function () {
        if (this.searchText !== '') {
            this.tokenTable = this.tokenTableSource.filter(item => {
                return item.symbol.toUpperCase().includes(this.searchText.toUpperCase())
            })
        } else {
            this.tokenTable = this.tokenTableSource;
        }
    },
    formatTime(time) {
        return moment(time).format('MM-DD');
    },
    sortTokenOnClick(row) {
        this.sortControl.desc = (this.sortControl.desc + 1) % 3;
        this.sortToken(row, this.sortControl.desc);
    },
    sortToken(row,desc) {
        this.sortControl.row = row;
        this.sortControl.desc = desc;
        if (this.sortControl.desc === 2){
            this.tokenTable.sort((a, b)=>{
                return parseFloat(b[row])-parseFloat(a[row])
            })
        }else if (this.sortControl.desc === 1) {
            this.tokenTable.sort((a, b)=>{
                return parseFloat(a[row])-parseFloat(b[row])
            })
        } else {
            this.tokenTable = this.tokenTableSource;
        }
    }
};

component.computed = {
    isSignedIn: function () {
        return !!app.account;
    }
};