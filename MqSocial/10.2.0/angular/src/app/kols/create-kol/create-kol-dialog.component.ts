import { Component, Injector, OnInit, EventEmitter, Output, ChangeDetectorRef } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import { KolServiceProxy, CreateKolDto, KolCareer, ChannelType } from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { AbpValidationSummaryComponent } from '../../../shared/components/validation/abp-validation.summary.component';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { DatePicker } from 'primeng/datepicker';
import { Select } from 'primeng/select';
import { InputNumber } from 'primeng/inputnumber';
import { Button } from 'primeng/button';
import moment from 'moment';

@Component({
    templateUrl: './create-kol-dialog.component.html',
    standalone: true,
    imports: [
        FormsModule,
        AbpModalHeaderComponent,
        AbpValidationSummaryComponent,
        LocalizePipe,
        InputText,
        Textarea,
        DatePicker,
        Select,
        InputNumber,
        Button,
    ],
})
export class CreateKolDialogComponent extends AppComponentBase implements OnInit {
    @Output() onSave = new EventEmitter<any>();

    saving = false;
    kol = new CreateKolDto();
    endDate: Date | null = null;

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
        this.kol.channel = ChannelType.Tiktok;
        this.kol.follow = 0;
    }

    save(): void {
        this.saving = true;
        this.kol.endDate = this.endDate ? moment(this.endDate) : undefined;

        this._kolService.create(this.kol).subscribe({
            next: () => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.bsModalRef.hide();
                this.onSave.emit();
            },
            error: () => { this.saving = false; },
        });
    }
}
