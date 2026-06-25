import { Component, Injector, OnInit, EventEmitter, Output, ChangeDetectorRef } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import { KolServiceProxy, KolDto, KolCareer, ChannelType } from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { AbpValidationSummaryComponent } from '../../../shared/components/validation/abp-validation.summary.component';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { Select } from 'primeng/select';
import { InputNumber } from 'primeng/inputnumber';
import { Button } from 'primeng/button';

@Component({
    templateUrl: './edit-kol-dialog.component.html',
    standalone: true,
    imports: [
        FormsModule,
        AbpModalHeaderComponent,
        AbpValidationSummaryComponent,
        LocalizePipe,
        InputText,
        Textarea,
        Select,
        InputNumber,
        Button,
    ],
})
export class EditKolDialogComponent extends AppComponentBase implements OnInit {
    @Output() onSave = new EventEmitter<any>();

    saving = false;
    kol = new KolDto();
    id: string;

    careerOptions = [
        { value: KolCareer.DuocSi, label: 'Dược sĩ' },
        { value: KolCareer.BacSi, label: 'Bác sĩ' },
        { value: KolCareer.Mom, label: 'Mẹ bé' },
    ];

    channelOptions = [
        { value: ChannelType.Tiktok, label: 'TikTok' },
        { value: ChannelType.Facebook, label: 'Facebook' },
    ];

    constructor(
        injector: Injector,
        public _kolService: KolServiceProxy,
        public bsModalRef: BsModalRef,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this._kolService.get(this.id).subscribe((result) => {
            this.kol = result;
            this.cd.detectChanges();
        });
    }

    save(): void {
        this.saving = true;
        this._kolService.update(this.kol).subscribe({
            next: () => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.bsModalRef.hide();
                this.onSave.emit();
            },
            error: () => { this.saving = false; },
        });
    }
}
